using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IGroqContentValidatorService
{
    Task<ContentValidationResult> ValidateContentAsync(string content, string requestedCategory = "educational");
}