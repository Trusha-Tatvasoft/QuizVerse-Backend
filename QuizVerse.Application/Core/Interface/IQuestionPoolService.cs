using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuestionPoolService
{
    Task<PageListResponse<QuestionPoolListDto>> GetQuestionPoolListAsync(PageListRequest pageListRequest);
}