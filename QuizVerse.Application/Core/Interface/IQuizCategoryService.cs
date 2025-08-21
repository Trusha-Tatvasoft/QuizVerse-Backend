using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
namespace QuizVerse.Application.Core.Interface;

public interface IQuizCategoryService
{
    Task<PageListResponse<QuizCategoryDTO>> GetQuizCategories(PageListRequest pageListRequest);
    public Task<QuizCategoryDTO> GetQuizCategoryById(int id);
    public Task<(bool Success, string Message)> CreateOrUpdateQuizCategory(QuizCategoryDTO quizCategoryDto);
    public Task<string> UpdateQuizCategoryByAction(QuizCategoryActionRequestDto quizCategoryAction);
    public Task<bool> IsCategoryNameAvailable(string name, int? id = null);
}