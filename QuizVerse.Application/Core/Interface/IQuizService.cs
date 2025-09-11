using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizService
{
    public Task<QuizOverviewResponseDto> GetQuizOverviewAsync(int quizId);
    public Task<QuizStartResponseDto?> StartQuizAsync(int quizId);
    public Task<QuizQuestionResponseDto> SaveAndNextQuestion(SaveAndNextQuestionRequestDto request);
}