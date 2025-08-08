using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizCategoryService
{
    Task<PageListResponse<QuizCategoryDTO>> GetQuizCategories(PageListRequest pageListRequest);
}
