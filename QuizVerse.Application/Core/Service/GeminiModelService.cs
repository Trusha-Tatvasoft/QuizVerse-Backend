using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Service;

public class GeminiModelService(IMemoryCache _memoryCache) : IGeminiModelService
{
    public async Task<AiModelName> GetNextAvailableModelAsync()
    {
        var rotationState = GetOrCreateRotationState();

        // Try models in priority order
        foreach (var model in Constants.GeminiAIModels.OrderBy(m => m.Priority))
        {
            if (await IsModelAvailableAsync(model.Name))
            {
                rotationState.CurrentActiveModel = model.Name;
                SaveRotationState(rotationState);
                return model.Name;
            }
        }

        throw new AppException(Constants.ALL_MODELS_RATE_LIMITED, StatusCodes.Status503ServiceUnavailable);
    }

    public async Task<bool> IsModelAvailableAsync(AiModelName modelName)
    {
        var rotationState = GetOrCreateRotationState();
        var model = Constants.GeminiAIModels.FirstOrDefault(m => m.Name == modelName);

        if (model == null) return false;

        if (!rotationState.ModelStats.ContainsKey(modelName))
        {
            rotationState.ModelStats[modelName] = new ModelUsageStats
            {
                LastResetDate = DateTime.Today
            };
        }

        var stats = rotationState.ModelStats[modelName];

        // Reset daily counter if it's a new day
        if (stats.LastResetDate < DateTime.Today)
        {
            stats.RequestsToday = 0;
            stats.LastResetDate = DateTime.Today;
            stats.RecentRequests.Clear();
        }

        // Check daily limit
        if (stats.RequestsToday >= (model.RPD - 1))
        {
            return false;
        }

        // Check minute limit - remove requests older than 1 minute
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        stats.RecentRequests = stats.RecentRequests.Where(t => t > oneMinuteAgo).ToList();

        if (stats.RecentRequests.Count >= (model.RPM - 1))
        {
            return false;
        }

        return true;
    }

    public void RecordRequestAsync(AiModelName modelName)
    {
        var rotationState = GetOrCreateRotationState();

        if (!rotationState.ModelStats.ContainsKey(modelName))
        {
            rotationState.ModelStats[modelName] = new ModelUsageStats
            {
                LastResetDate = DateTime.Today
            };
        }

        var stats = rotationState.ModelStats[modelName];

        // Reset if new day
        if (stats.LastResetDate < DateTime.Today)
        {
            stats.RequestsToday = 0;
            stats.LastResetDate = DateTime.Today;
            stats.RecentRequests.Clear();
        }

        stats.RequestsToday++;
        stats.RecentRequests.Add(DateTime.UtcNow);

        SaveRotationState(rotationState);
    }

    private ModelRotationState GetOrCreateRotationState()
    {
        if (!_memoryCache.TryGetValue(Constants.ROTATION_STATE_CACHE_KEY, out ModelRotationState rotationState))
        {
            rotationState = new ModelRotationState();
            SaveRotationState(rotationState);
        }

        return rotationState;
    }

    private void SaveRotationState(ModelRotationState rotationState)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(Constants.CACHE_EXPIRATION_HOURS))
            .SetAbsoluteExpiration(TimeSpan.FromHours(Constants.CACHE_EXPIRATION_HOURS));

        _memoryCache.Set(Constants.ROTATION_STATE_CACHE_KEY, rotationState, cacheOptions);
    }
}