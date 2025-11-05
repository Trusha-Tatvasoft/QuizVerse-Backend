using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IContentModerationService
{
    public Task<PageListResponse<QuizReportIssueResponseDTO>> GetQuizReportByPaginationAsync(PageListRequest query);
    Task<PageListResponse<FlaggedCommentDto>> GetFlaggedComments(PageListRequest request);
    Task<FlaggedCommentViewDto> GetFlaggedCommentById(int id);
    Task UpdateFlaggedCommentStatus(UpdateFlaggedCommentStatusRequest request);
}
