using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IAiQuestionGenerationService
{
    Task<GenerateQuizResponseDto> GenerateFromPromptAsync(GenerateQuizRequest request);
    Task<GenerateQuizResponseDto> GenerateQuestionUsingWebURL(GenerateQuestionUsingWebRequestDTO request);
    Task<GenerateQuizResponseDto> GenerateFromPdfAsync(GenerateQuizFromPDFRequest request);
}