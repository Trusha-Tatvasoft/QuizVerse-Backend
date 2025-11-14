using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Interface;

public interface IGeminiModelService
{
    Task<AiModelName> GetNextAvailableModelAsync();
    void RecordRequestAsync(AiModelName modelName);
    Task<bool> IsModelAvailableAsync(AiModelName modelName);
}
