using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizManagementService
{
    // Task<QuizManagementPageDataDto> GetQuizCardData();
    // Task<PageListResponse<QuizListDto>> GetQuizzesByPagination(PageListRequest pageListRequest);
    Task MoveQuizzesToCategoryAsync(QuizCategory quizCategoryWithQuizzes, int toCategoryId);
    Task<CreateUpdateResponseDto> CreateUpdateQuiz(SaveQuizRequestDto quizCreateUpdateRequestDto);
    Task<QuizResponseDto> GetQuizDataById(int quizId);
}
