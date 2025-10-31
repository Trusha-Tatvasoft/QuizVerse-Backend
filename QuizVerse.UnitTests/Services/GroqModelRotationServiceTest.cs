using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using Xunit;
using System.Collections.Concurrent;

namespace QuizVerse.UnitTests.Services;

public class GroqModelRotationServiceTests
{
    private readonly GroqModelRotationService _rotationService;

    public GroqModelRotationServiceTests()
    {
        _rotationService = new GroqModelRotationService();

        // Reset all models before each test to ensure clean state
        ResetAllModels();
    }

    [Fact]
    public void GetAvailableModel_WithNoLimitsReached_ReturnsFirstModel()
    {
        // Act
        var model = _rotationService.GetAvailableModel();

        // Assert
        Assert.NotNull(model);
        Assert.Equal("llama-3.1-8b-instant", model.ModelName);
    }

    [Fact]
    public void GetAvailableModel_WhenCurrentModelLimitReached_RotatesToNextAvailableModel()
    {
        // Arrange
        var firstModel = _rotationService.GetAvailableModel();
        Assert.NotNull(firstModel);

        // Simulate first model reaching its minute token limit
        SetModelTokenUsage(firstModel, firstModel.TokensPerMinute + 100);

        // Act
        var nextModel = _rotationService.GetAvailableModel();

        // Assert
        Assert.NotNull(nextModel);
        Assert.NotEqual(firstModel.ModelName, nextModel.ModelName);
        // Could be any available model, not necessarily the second one
    }

    [Fact]
    public void GetAvailableModel_WhenAllModelsLimitReached_ReturnsNull()
    {
        // Arrange
        // Exhaust all models
        var models = GetModelsViaReflection();
        foreach (var model in models)
        {
            SetModelTokenUsage(model, model.TokensPerMinute + 100);
        }

        // Act
        var result = _rotationService.GetAvailableModel();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ForceRotateToNextModel_ChangesCurrentModelIndex()
    {
        // Arrange
        var firstModel = _rotationService.GetAvailableModel();
        Assert.NotNull(firstModel);
        var firstModelName = firstModel.ModelName;

        // Act
        _rotationService.ForceRotateToNextModel();
        var nextModel = _rotationService.GetAvailableModel();

        // Assert
        Assert.NotNull(nextModel);
        Assert.NotEqual(firstModelName, nextModel.ModelName);
    }

    [Fact]
    public void ForceRotateToNextModel_WrapsAroundToFirstModel()
    {
        // Arrange
        var firstModelName = _rotationService.GetAvailableModel()?.ModelName;
        Assert.NotNull(firstModelName);

        // Rotate through all models
        var modelsCount = GetModelsViaReflection().Count;
        for (int i = 0; i < modelsCount; i++)
        {
            _rotationService.ForceRotateToNextModel();
        }

        // Act
        var model = _rotationService.GetAvailableModel();

        // Assert
        Assert.NotNull(model);
        Assert.Equal(firstModelName, model.ModelName);
    }

    [Fact]
    public void RecordUsage_WithValidModel_UpdatesTokenUsage()
    {
        // Arrange
        var modelName = "llama-3.1-8b-instant";
        var initialModel = GetModelByName(modelName);
        Assert.NotNull(initialModel);
        var initialTokens = initialModel.TokensUsedPerMinute;

        // Act
        _rotationService.RecordUsage(modelName, 100);

        // Assert
        var modelAfterUsage = GetModelByName(modelName);
        Assert.NotNull(modelAfterUsage);
        Assert.Equal(initialTokens + 100, modelAfterUsage.TokensUsedPerMinute);
    }

    [Fact]
    public void RecordUsage_WithInvalidModel_ThrowsArgumentException()
    {
        // Arrange
        var invalidModelName = "invalid-model";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _rotationService.RecordUsage(invalidModelName, 100));

        Assert.Contains("Unknown model", exception.Message);
        Assert.Equal("modelName", exception.ParamName);
    }

    [Fact]
    public void HandleRateLimitExceeded_WithValidModel_BlocksModelAndRotates()
    {
        // Arrange
        var modelName = "llama-3.1-8b-instant";
        var initialModel = _rotationService.GetAvailableModel();
        Assert.NotNull(initialModel);

        // Act
        _rotationService.HandleRateLimitExceeded(modelName);

        // Assert
        var nextModel = _rotationService.GetAvailableModel();
        Assert.NotNull(nextModel);
        Assert.NotEqual(modelName, nextModel.ModelName);

        // Verify the original model is now blocked/limited
        var blockedModel = GetModelByName(modelName);
        Assert.NotNull(blockedModel);
        Assert.True(blockedModel.IsLimitReached());
    }

    [Fact]
    public void HandleRateLimitExceeded_WithInvalidModel_ThrowsArgumentException()
    {
        // Arrange
        var invalidModelName = "invalid-model";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _rotationService.HandleRateLimitExceeded(invalidModelName));

        Assert.Contains("Unknown model for rate limit", exception.Message);
        Assert.Equal("modelName", exception.ParamName);
    }

    [Fact]
    public void ResetExpiredLimits_AfterMinuteReset_ResetsPerMinuteUsage()
    {
        // Arrange
        var modelName = "llama-3.1-8b-instant";

        // Record usage to set some token usage
        _rotationService.RecordUsage(modelName, 1000);

        var modelBeforeReset = GetModelByName(modelName);
        Assert.NotNull(modelBeforeReset);

        // Manually set the minute reset time to be in the past to trigger reset
        SetMinuteResetTime(modelName, DateTime.UtcNow.AddMinutes(-1));

        // Act - Reset expired limits which should reset per-minute usage
        ResetExpiredLimitsViaReflection();

        // Assert
        var modelAfterReset = GetModelByName(modelName);
        Assert.NotNull(modelAfterReset);
        Assert.Equal(0, modelAfterReset.TokensUsedPerMinute);
    }

    [Fact]
    public void ModelRotation_CircularBehavior_ReturnsToFirstModelAfterFullCycle()
    {
        // Arrange & Act
        var firstModel = _rotationService.GetAvailableModel();
        Assert.NotNull(firstModel);
        var firstModelName = firstModel.ModelName;

        // Rotate through all models
        var modelsCount = GetModelsViaReflection().Count;
        for (int i = 0; i < modelsCount; i++)
        {
            _rotationService.ForceRotateToNextModel();
        }

        // Should return to first model
        var finalModel = _rotationService.GetAvailableModel();

        // Assert
        Assert.NotNull(finalModel);
        Assert.Equal(firstModelName, finalModel.ModelName);
    }

    [Fact]
    public void GetAvailableModel_AfterRateLimit_RotatesThroughAllModels()
    {
        // Arrange
        // Get all model names except one
        var allModels = GetModelsViaReflection();
        var modelsToLimit = allModels.Take(allModels.Count - 1).Select(m => m.ModelName).ToList();

        foreach (var modelName in modelsToLimit)
        {
            _rotationService.HandleRateLimitExceeded(modelName);
        }

        // Act
        var availableModel = _rotationService.GetAvailableModel();

        // Assert
        Assert.NotNull(availableModel);
        // Should be the last model that wasn't rate limited
        Assert.DoesNotContain(availableModel.ModelName, modelsToLimit);
    }

    [Fact]
    public void RecordUsage_UpdatesBothPerMinuteAndPerDayUsage()
    {
        // Arrange
        var modelName = "llama-3.1-8b-instant";
        var initialModel = GetModelByName(modelName);
        Assert.NotNull(initialModel);
        var initialMinuteUsage = initialModel.TokensUsedPerMinute;
        var initialDayUsage = initialModel.TokensUsedPerDay;

        // Act
        _rotationService.RecordUsage(modelName, 150);

        // Assert
        var updatedModel = GetModelByName(modelName);
        Assert.NotNull(updatedModel);
        Assert.Equal(initialMinuteUsage + 150, updatedModel.TokensUsedPerMinute);
        Assert.Equal(initialDayUsage + 150, updatedModel.TokensUsedPerDay);
    }

    [Fact]
    public void HandleRateLimitExceeded_WhenCurrentModel_RotatesImmediately()
    {
        // Arrange
        var currentModel = _rotationService.GetAvailableModel();
        Assert.NotNull(currentModel);
        var currentModelName = currentModel.ModelName;

        // Act
        _rotationService.HandleRateLimitExceeded(currentModelName);

        // Assert
        var nextModel = _rotationService.GetAvailableModel();
        Assert.NotNull(nextModel);
        Assert.NotEqual(currentModelName, nextModel.ModelName);
    }

    [Fact]
    public void HandleRateLimitExceeded_WhenNotCurrentModel_DoesNotChangeCurrentIndex()
    {
        // Arrange
        var currentModel = _rotationService.GetAvailableModel();
        Assert.NotNull(currentModel);
        var currentModelName = currentModel.ModelName;

        // Find a model that is not the current one
        var nonCurrentModel = GetModelsViaReflection().First(m => m.ModelName != currentModelName);
        var nonCurrentModelName = nonCurrentModel.ModelName;

        // Act
        _rotationService.HandleRateLimitExceeded(nonCurrentModelName);

        // Assert
        var stillCurrentModel = _rotationService.GetAvailableModel();
        Assert.NotNull(stillCurrentModel);
        Assert.Equal(currentModelName, stillCurrentModel.ModelName);
    }

    [Fact]
    public void GetAvailableModel_AfterResetExpiredLimits_WithMinuteReset_ResetsToFirstModel()
    {
        // Arrange
        // Force rotate to a different model first
        _rotationService.ForceRotateToNextModel();
        var secondModel = _rotationService.GetAvailableModel();
        Assert.NotNull(secondModel);
        var firstModelName = GetModelsViaReflection()[0].ModelName;

        // Set all minute reset times to be in the past to trigger reset
        var models = GetModelsViaReflection();
        foreach (var model in models)
        {
            SetMinuteResetTime(model.ModelName, DateTime.UtcNow.AddMinutes(-1));
        }

        // Act - Reset expired limits which should reset to first model
        ResetExpiredLimitsViaReflection();

        // Assert
        var modelAfterReset = _rotationService.GetAvailableModel();
        Assert.NotNull(modelAfterReset);
        Assert.Equal(firstModelName, modelAfterReset.ModelName);
    }

    [Fact]
    public void MultipleRecordUsage_CumulativeTracking()
    {
        // Arrange
        var modelName = "llama-3.1-8b-instant";
        var initialModel = GetModelByName(modelName);
        Assert.NotNull(initialModel);
        var initialTokens = initialModel.TokensUsedPerMinute;

        // Act
        _rotationService.RecordUsage(modelName, 100);
        _rotationService.RecordUsage(modelName, 200);
        _rotationService.RecordUsage(modelName, 150);

        // Assert
        var model = GetModelByName(modelName);
        Assert.NotNull(model);
        Assert.Equal(initialTokens + 450, model.TokensUsedPerMinute);
        Assert.Equal(initialTokens + 450, model.TokensUsedPerDay);
    }

    // Helper method to reset all models to clean state
    private void ResetAllModels()
    {
        var models = GetModelsViaReflection();
        foreach (var model in models)
        {
            SetModelTokenUsage(model, 0, 0);

            // Reset any blocking
            var blockedUntilField = typeof(ModelConfig).GetField("_blockedUntil",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            blockedUntilField?.SetValue(model, null);
        }

        // Reset current index
        var currentIndexField = typeof(GroqModelRotationService).GetField("_currentModelIndex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        currentIndexField?.SetValue(_rotationService, 0);

        // Reset reset times
        var now = DateTime.UtcNow;
        var minuteResetTimesField = typeof(GroqModelRotationService).GetField("_minuteResetTimes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dayResetTimesField = typeof(GroqModelRotationService).GetField("_dayResetTimes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (minuteResetTimesField?.GetValue(_rotationService) is ConcurrentDictionary<string, DateTime> minuteResetTimes)
        {
            foreach (var model in models)
            {
                minuteResetTimes[model.ModelName] = now.AddMinutes(1);
            }
        }

        if (dayResetTimesField?.GetValue(_rotationService) is ConcurrentDictionary<string, DateTime> dayResetTimes)
        {
            foreach (var model in models)
            {
                dayResetTimes[model.ModelName] = now.Date.AddDays(1);
            }
        }
    }

    // Helper method to get a model by name using reflection
    private ModelConfig? GetModelByName(string modelName)
    {
        var models = GetModelsViaReflection();
        return models.FirstOrDefault(m => m.ModelName == modelName);
    }

    // Helper method to get all models using reflection
    private List<ModelConfig> GetModelsViaReflection()
    {
        var modelsField = typeof(GroqModelRotationService).GetField("_models",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (modelsField?.GetValue(_rotationService) is List<ModelConfig> models)
        {
            return models;
        }

        return new List<ModelConfig>();
    }

    // Helper method to call ResetExpiredLimits via reflection
    private void ResetExpiredLimitsViaReflection()
    {
        var resetMethod = typeof(GroqModelRotationService).GetMethod("ResetExpiredLimits",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        resetMethod?.Invoke(_rotationService, null);
    }

    // Helper method to set token usage using reflection
    private void SetModelTokenUsage(ModelConfig model, int tokensUsedPerMinute, int tokensUsedPerDay = 0)
    {
        var tokensUsedPerMinuteField = typeof(ModelConfig).GetField("<TokensUsedPerMinute>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var tokensUsedPerDayField = typeof(ModelConfig).GetField("<TokensUsedPerDay>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        tokensUsedPerMinuteField?.SetValue(model, tokensUsedPerMinute);
        tokensUsedPerDayField?.SetValue(model, tokensUsedPerDay);
    }

    // Helper method to set minute reset time using reflection
    private void SetMinuteResetTime(string modelName, DateTime resetTime)
    {
        var minuteResetTimesField = typeof(GroqModelRotationService).GetField("_minuteResetTimes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (minuteResetTimesField?.GetValue(_rotationService) is ConcurrentDictionary<string, DateTime> minuteResetTimes)
        {
            minuteResetTimes[modelName] = resetTime;
        }
    }
}