using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IContentModerationService
{
    public Task<PageListResponse<QuizReportIssueResponseDTO>> GetQuizReportByPaginationAsync(PageListRequest query);
    public Task<string> UpdateQuizReportAction(QuizAndQuestionReportAction actionRequest);
    public Task<ContentModerationMetricsDataDto> GetContentModerationMatricsData();
    public Task<PageListResponse<QuestionIssueReportDTO>> GetQuestionReportByPaginationAsync(PageListRequest query);
    public Task<string> UpdateQuestionReportAction(QuizAndQuestionReportAction actionRequest);
    public Task<QuestionIssuePreviewRequestDto> GetQuestionIssueReportPreview(int queId);
    public Task<List<ActiveQuizBattleAffectedDTO>> GetAffectedQuizAndBattle(int queId);
    public Task<string> UpdateReportedQuestion(int reportId,QuestionRequestDTO dto);
    Task<PageListResponse<FlaggedCommentDto>> GetFlaggedComments(PageListRequest request);
    Task<FlaggedCommentViewDto> GetFlaggedCommentById(int id);
    Task UpdateFlaggedCommentStatus(UpdateFlaggedCommentStatusRequest request);
}
