using AutoMapper;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using AutoMapper.QueryableExtensions;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Enums;
using Npgsql;
using NpgsqlTypes;

namespace QuizVerse.Application.Core.Service;

public class ContentModerationService(IGenericRepository<QuizIssueReport> _reportedQuizRepository, IHttpContextAccessor _httpContextAccessor, IMapper _mapper, ISqlQueryRepository sqlQueryRepository, IGenericRepository<QuizRating> quizRatingRepository) : IContentModerationService
{
    public string UserRole => _httpContextAccessor.HttpContext?.User?.GetUserRole() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    public int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region GetQuizReportByPaginationAsync
    private IQueryable<QuizIssueReport> GetQuizReportData(PageListRequest query)
    {
        IQueryable<QuizIssueReport> quizIssueReportsQuery = _reportedQuizRepository
            .GetQueryableInclude(q => q.Quiz, q => q.User, q => q.Quiz.CreatedByNavigation);

        // Sorting
        if (!string.IsNullOrEmpty(query.SortColumn))
        {
            query.SortColumn = query.SortColumn.ToLower() switch
            {
                "quiz" => "Quiz.Name",
                "creator" => "Quiz.CreatedByNavigation.FullName",
                "reporter" => "User.FullName",
                "severity" => "Severity",
                _ => "Id",
            };
            quizIssueReportsQuery = quizIssueReportsQuery
                .OrderBy($"{query.SortColumn} {(query.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            quizIssueReportsQuery = quizIssueReportsQuery.OrderBy("Id asc");
        }

        var filters = query.Filters;
        if (filters != null)
        {
            if (filters.IssueReportSeverity.HasValue)
            {
                if (Enum.IsDefined(typeof(QuestionOrQuizIssueReportSeverity), filters.IssueReportSeverity.Value))
                {
                    quizIssueReportsQuery = quizIssueReportsQuery.Where(u => u.Severity == (int)filters.IssueReportSeverity.Value);
                }
                else
                {
                    throw new AppException(Constants.INVALID_SEVERITY_MESSAGE);
                }
            }
            if (filters.IssueReportStatus.HasValue)
            {
                if (Enum.IsDefined(typeof(QuestionOrQuizIssueReportStatus), filters.IssueReportStatus.Value))
                {
                    quizIssueReportsQuery = quizIssueReportsQuery.Where(u => u.Status == (int)filters.IssueReportStatus.Value);
                }
                else
                {
                    throw new AppException(Constants.INVALID_ROLE_MESSAGE);
                }
            }
        }
        return quizIssueReportsQuery;
    }

    public async Task<PageListResponse<QuizReportIssueResponseDTO>> GetQuizReportByPaginationAsync(PageListRequest query)
    {
        IQueryable<QuizIssueReport> quizReportQuery = GetQuizReportData(query);

        return await _reportedQuizRepository.PaginatedList(
            quizReportQuery,
            query,
            q => q.ProjectTo<QuizReportIssueResponseDTO>(_mapper.ConfigurationProvider)
        );
    }
    #endregion

    #region Flagged Comments
    public async Task<PageListResponse<FlaggedCommentDto>> GetFlaggedComments(PageListRequest request)
    {
        string sortDirection = request.SortDescending ? "DESC" : "ASC";

        string query = string.Format(SqlConstants.GET_FLAGGED_COMMENTS_QUERY_TEMPLATE, SqlConstants.GET_FLAGGED_COMMENTS_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_page_number", NpgsqlDbType.Integer) { Value = request.PageNumber },
            new("p_page_size", NpgsqlDbType.Integer) { Value = request.PageSize },
            new("p_sort_column", NpgsqlDbType.Text) { Value = request.SortColumn },
            new("p_sort_direction", NpgsqlDbType.Text) { Value = sortDirection },
            new("p_status_filter", NpgsqlDbType.Integer) { Value = (object?)(int?)request.Filters?.CommentStatus ?? DBNull.Value }
        };

        var result = await sqlQueryRepository
            .SqlQuerySingleAsync<FlaggedCommentsResultDTO>(query, parameters);

        var response = new PageListResponse<FlaggedCommentDto>
        {
            TotalRecords = result.TotalRecords,
            Records = []
        };

        if (!string.IsNullOrEmpty(result.Records))
        {
            response.Records = JsonSerializer.Deserialize<List<FlaggedCommentDto>>(
                result.Records,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? [];
        }

        return response;
    }

    public async Task<FlaggedCommentViewDto> GetFlaggedCommentById(int id)
    {
        string query = string.Format(SqlConstants.GET_FLAGGED_COMMENT_By_Id_QUERY_TEMPLATE, SqlConstants.GET_FLAGGED_COMMENT_By_Id_FUNCTION);

        var parameters = new[]
        {
            new NpgsqlParameter("p_id", NpgsqlDbType.Integer) { Value = id }
        };

        return await sqlQueryRepository.SqlQuerySingleAsync<FlaggedCommentViewDto>(query, parameters);
    }

    public async Task UpdateFlaggedCommentStatus(UpdateFlaggedCommentStatusRequest request)
    {
        var comment = await quizRatingRepository.GetAsync(qr => qr.Id == request.Id && qr.IsFlagged) ?? throw new AppException(Constants.FLAGGED_COMMENT_NOT_FOUND);

        if (comment.Status == (int)QuizRatingStatus.Accepted || comment.Status == (int)QuizRatingStatus.Ignore)
        {
            throw new AppException(Constants.CAN_NOT_UPDATE_STATUS_COMMENT);
        }

        comment.Status = request.Status;
        comment.ModifiedBy = UserId;
        comment.ModifiedDate = DateTime.UtcNow;

        await quizRatingRepository.UpdateAsync(comment);
    }
    #endregion
}