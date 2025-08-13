using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuestionPoolService
{
    Task<PageListResponse<QuestionPoolListDto>> GetQuestionPoolListAsync(PageListRequest pageListRequest);

    Task<string> CreateOrUpdateQuestion(int id, QuestionRequestDTO dto);

    Task<string> DeleteQuestion(int id);

    Task<QuestionDetailDTO?> GetQuestionPreview(int id);

    Task<string> ImportQuestionsFromCsv(Stream fileStream);

    Task<string> ImportQuestionsFromExcel(Stream fileStream);
}
