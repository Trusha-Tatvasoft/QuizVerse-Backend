using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizManagmentService
{
    Task<QuizManagmentPageDataDto> GetQuizCardData();
    Task<PageListResponse<QuizListDto>> GetQuizzesByPagination(PageListRequest pageListRequest);
}
