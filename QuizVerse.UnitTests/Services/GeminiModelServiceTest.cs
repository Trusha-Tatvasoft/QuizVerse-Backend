using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class GeminiModelServiceTest
    {
        private readonly IMemoryCache _cache;
        private readonly GeminiModelService _service;

        public GeminiModelServiceTest()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new GeminiModelService(_cache);
        }

        [Fact]
        public async Task GetNextAvailableModelAsync_ShouldReturn_FirstAvailableModel()
        {
            // Act
            var model = await _service.GetNextAvailableModelAsync();

            // Assert
            Assert.Contains(model, Constants.GeminiAIModels.Select(m => m.Name));
            var rotationStateExists = _cache.TryGetValue(Constants.ROTATION_STATE_CACHE_KEY, out _);
            Assert.True(rotationStateExists);
        }

        [Fact]
        public async Task GetNextAvailableModelAsync_ShouldThrow_WhenAllRateLimited()
        {
            // Arrange
            var rotationState = new ModelRotationState();
            foreach (var model in Constants.GeminiAIModels)
            {
                rotationState.ModelStats[model.Name] = new ModelUsageStats
                {
                    RequestsToday = model.RPD,
                    LastResetDate = DateTime.Today
                };
            }
            _cache.Set(Constants.ROTATION_STATE_CACHE_KEY, rotationState);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.GetNextAvailableModelAsync());
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, ex.StatusCode);
        }

        [Fact]
        public async Task IsModelAvailableAsync_ShouldReturnFalse_WhenModelNotExists()
        {
            var result = await _service.IsModelAvailableAsync((AiModelName)999);
            Assert.False(result);
        }

        [Fact]
        public async Task IsModelAvailableAsync_ShouldResetStats_WhenNewDay()
        {
            var rotationState = new ModelRotationState();
            var target = Constants.GeminiAIModels.First().Name;

            rotationState.ModelStats[target] = new ModelUsageStats
            {
                LastResetDate = DateTime.Today.AddDays(-1),
                RequestsToday = 5,
                RecentRequests = new List<DateTime> { DateTime.UtcNow.AddMinutes(-3) }
            };

            _cache.Set(Constants.ROTATION_STATE_CACHE_KEY, rotationState);

            var result = await _service.IsModelAvailableAsync(target);

            Assert.True(result);
            var state = _cache.Get<ModelRotationState>(Constants.ROTATION_STATE_CACHE_KEY);
            var stats = state.ModelStats[target];
            Assert.Equal(0, stats.RequestsToday);
            Assert.Empty(stats.RecentRequests);
            Assert.Equal(DateTime.Today, stats.LastResetDate);
        }

        [Fact]
        public async Task IsModelAvailableAsync_ShouldReturnFalse_WhenMinuteLimitExceeded()
        {
            var model = Constants.GeminiAIModels.First();
            var rotationState = new ModelRotationState();
            rotationState.ModelStats[model.Name] = new ModelUsageStats
            {
                LastResetDate = DateTime.Today,
                RequestsToday = 1,
                RecentRequests = Enumerable.Repeat(DateTime.UtcNow, model.RPM).ToList()
            };

            _cache.Set(Constants.ROTATION_STATE_CACHE_KEY, rotationState);

            var available = await _service.IsModelAvailableAsync(model.Name);
            Assert.False(available);
        }

        [Fact]
        public void RecordRequestAsync_ShouldIncrementRequests_AndSaveToCache()
        {
            var model = Constants.GeminiAIModels.First().Name;

            _service.RecordRequestAsync(model);

            var state = _cache.Get<ModelRotationState>(Constants.ROTATION_STATE_CACHE_KEY);
            var stats = state.ModelStats[model];

            Assert.Equal(1, stats.RequestsToday);
            Assert.True(stats.RecentRequests.Count == 1);
            Assert.Equal(DateTime.Today, stats.LastResetDate);
        }

        [Fact]
        public void RecordRequestAsync_ShouldResetCounters_WhenNewDay()
        {
            var model = Constants.GeminiAIModels.First().Name;

            var rotationState = new ModelRotationState();
            rotationState.ModelStats[model] = new ModelUsageStats
            {
                LastResetDate = DateTime.Today.AddDays(-1),
                RequestsToday = 10
            };

            _cache.Set(Constants.ROTATION_STATE_CACHE_KEY, rotationState);

            _service.RecordRequestAsync(model);

            var state = _cache.Get<ModelRotationState>(Constants.ROTATION_STATE_CACHE_KEY);
            var stats = state.ModelStats[model];

            Assert.Equal(1, stats.RequestsToday); // reset + increment
            Assert.Equal(DateTime.Today, stats.LastResetDate);
        }
    }
}
