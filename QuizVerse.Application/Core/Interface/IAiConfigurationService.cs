using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Interface;

public interface IAiConfigurationService
{
    Task<AiConfigurationCardDetailsDTO> GetAiConfigurationCardDetails();
    Task<AiUsesDetailsDTO> GetAiUsesDetails(AiModelName? aiModelName);
}
