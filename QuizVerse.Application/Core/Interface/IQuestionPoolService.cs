using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuestionPoolService
{
    Task<PageListResponse<QuestionPoolListDto>> GetQuestionPoolListAsync(PageListRequest pageListRequest);

    Task<string> CreateOrUpdateQuestion(int id, QuestionRequestDTO dto);

    Task<string> DeleteQuestion(int id);

    Task<QuestionDetailDTO?> GetQuestionPreview(int id);

    Task<string> SaveQuestions(List<QuestionsListRequestDto> questionList);

    Task<List<QuestionsListResponseDto>> PreviewQuestionsFromCsv(Stream fileStream);

    Task<List<QuestionsListResponseDto>> PreviewQuestionsFromExcel(Stream fileStream);
}
