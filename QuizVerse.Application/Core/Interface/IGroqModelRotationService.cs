using QuizVerse.Infrastructure.Common;

namespace QuizVerse.Application.Core.Interface;

public interface IGroqModelRotationService
{
    ModelConfig? GetAvailableModel();
    void ForceRotateToNextModel();
    void RecordUsage(string modelName, int tokensUsed);
    void HandleRateLimitExceeded(string modelName);
}