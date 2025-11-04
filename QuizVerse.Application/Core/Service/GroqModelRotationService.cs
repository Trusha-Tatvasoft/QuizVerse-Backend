using System.Collections.Concurrent;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.Application.Core.Service;

public class GroqModelRotationService : IGroqModelRotationService
{
    private readonly List<ModelConfig> _models;
    private readonly ConcurrentDictionary<string, DateTime> _minuteResetTimes;
    private readonly ConcurrentDictionary<string, DateTime> _dayResetTimes;
    private int _currentModelIndex = 0;
    private readonly object _lock = new();

    public GroqModelRotationService()
    {
        _minuteResetTimes = new ConcurrentDictionary<string, DateTime>();
        _dayResetTimes = new ConcurrentDictionary<string, DateTime>();

        _models = Constants.GroqModels;

        var now = DateTime.UtcNow;
        foreach (var model in _models)
        {
            _minuteResetTimes[model.ModelName] = now.AddMinutes(1);
            _dayResetTimes[model.ModelName] = now.Date.AddDays(1);
        }
    }

    public ModelConfig? GetAvailableModel()
    {
        lock (_lock)
        {
            ResetExpiredLimits();

            var currentModel = _models[_currentModelIndex];

            if (!currentModel.IsLimitReached())
            {
                return currentModel;
            }

            int startIndex = _currentModelIndex;
            int modelsChecked = 0;

            while (modelsChecked < _models.Count)
            {
                _currentModelIndex = (_currentModelIndex + 1) % _models.Count;
                modelsChecked++;

                var model = _models[_currentModelIndex];

                if (!model.IsLimitReached())
                {
                    return model;
                }
            }

            return null;
        }
    }

    public void ForceRotateToNextModel()
    {
        lock (_lock)
        {
            _currentModelIndex = (_currentModelIndex + 1) % _models.Count;
        }
    }

    public void RecordUsage(string modelName, int tokensUsed)
    {
        lock (_lock)
        {
            var model = _models.FirstOrDefault(m => m.ModelName == modelName);
            if (model == null)
            {
                throw new ArgumentException($"Unknown model: {modelName}", nameof(modelName));
            }

            model.RecordRequest(tokensUsed);
        }
    }

    public void HandleRateLimitExceeded(string modelName)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var model = _models.FirstOrDefault(m => m.ModelName == modelName);

            if (model == null)
            {
                throw new ArgumentException($"Unknown model for rate limit: {modelName}", nameof(modelName));
            }

            var nextMinuteReset = _minuteResetTimes[model.ModelName];
            var blockUntil = now.AddSeconds(30);

            if (nextMinuteReset > blockUntil)
            {
                blockUntil = nextMinuteReset;
            }

            _minuteResetTimes[model.ModelName] = blockUntil;

            var blockDuration = (blockUntil - now).TotalSeconds;
            model.MarkAsTemporarilyBlocked((int)blockDuration);

            var currentIndex = _models.FindIndex(m => m.ModelName == modelName);
            if (currentIndex >= 0 && currentIndex == _currentModelIndex)
            {
                _currentModelIndex = (_currentModelIndex + 1) % _models.Count;
            }
        }
    }

    private void ResetExpiredLimits()
    {
        var now = DateTime.UtcNow;
        bool anyMinuteReset = false;

        foreach (var model in _models)
        {
            if (_minuteResetTimes[model.ModelName] <= now)
            {
                model.ResetPerMinuteUsage();
                _minuteResetTimes[model.ModelName] = now.AddMinutes(1);
                anyMinuteReset = true;
            }

            if (_dayResetTimes[model.ModelName] <= now)
            {
                model.ResetPerDayUsage();
                _dayResetTimes[model.ModelName] = now.Date.AddDays(1);
            }
        }

        if (anyMinuteReset && _currentModelIndex != 0)
        {
            _currentModelIndex = 0;
        }
    }
}