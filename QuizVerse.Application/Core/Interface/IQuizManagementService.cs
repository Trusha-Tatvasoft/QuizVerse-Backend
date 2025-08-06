using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizManagementService
{
    Task<QuizManagementPageDataDto> GetQuizCardData();
    Task<PageListResponse<QuizListDto>> GetQuizzesByPagination(PageListRequest pageListRequest);
}
