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
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Service;

public class ContentModerationService(
    ISqlQueryRepository _sqlQueryRepository,
    IGenericRepository<QuizIssueReport> _reportedQuizRepository,
    IGenericRepository<QuestionIssueReport> _reportedQuestionRepository,
    IQuestionPoolService _questionPoolService,
    IHttpContextAccessor _httpContextAccessor,
    IMapper _mapper, ISqlQueryRepository sqlQueryRepository, IGenericRepository<QuizRating> quizRatingRepository) : IContentModerationService
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

    #region GetContentModerationMatricsData
    public async Task<ContentModerationMetricsDataDto> GetContentModerationMatricsData()
    {
        return await _sqlQueryRepository.SqlQuerySingleAsync<ContentModerationMetricsDataDto>(SqlConstants.GET_CONTENT_MODERATION_METRICS);
    }
    #endregion

    #region GetQuestionReportByPaginationAsync
    public async Task<PageListResponse<QuestionIssueReportDTO>> GetQuestionReportByPaginationAsync(PageListRequest pageListRequest)
    {
        string query = string.Format(SqlConstants.GET_CONTENT_MODERATION_QUESTION_REPORT_LIST_TEMPLATE, SqlConstants.GET_CONTENT_MODERATION_QUESTION_REPORT_LIST_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_page_number", NpgsqlDbType.Integer) { Value = pageListRequest.PageNumber },
            new("p_page_size", NpgsqlDbType.Integer) { Value = pageListRequest.PageSize },
            new("p_sort_column", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SortColumn ?? DBNull.Value },
            new("p_sort_descending", NpgsqlDbType.Boolean) { Value = pageListRequest.SortDescending },
            new("p_severity", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.Severity != null? (int)pageListRequest.Filters.Severity:DBNull.Value },
            new("p_status", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuestionOrQuizIssueReportStatus != null?(int)pageListRequest.Filters.QuestionOrQuizIssueReportStatus : DBNull.Value },
        };

        string queryForTotalCount = string.Format(SqlConstants.GET_CONTENT_MODERATION_QUESTION_REPORT_TOTAL_COUNT_TEMPLATE, SqlConstants.GET_CONTENT_MODERATION_QUESTION_REPORT_TOTAL_COUNT_FUNCTION);

        var parametersForTotalCount = new NpgsqlParameter[]
        {
            new("p_page_number", NpgsqlDbType.Integer) { Value = pageListRequest.PageNumber },
            new("p_page_size", NpgsqlDbType.Integer) { Value = pageListRequest.PageSize },
            new("p_severity", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.Severity != null? (int)pageListRequest.Filters.Severity:DBNull.Value },
            new("p_status", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuestionOrQuizIssueReportStatus != null?(int)pageListRequest.Filters.QuestionOrQuizIssueReportStatus : DBNull.Value },
          };

        List<QuestionIssueReportDTO> questionPools = await _sqlQueryRepository.SqlQueryListAsync<QuestionIssueReportDTO>(query, parameters);
        TotalRecordsDto totalRecords = await _sqlQueryRepository.SqlQuerySingleAsync<TotalRecordsDto>(queryForTotalCount, parametersForTotalCount);

        PageListResponse<QuestionIssueReportDTO> response = new()
        {
            TotalRecords = totalRecords.TotalRecords,
            Records = questionPools
        };

        return response;
    }
    #endregion

    #region QuestionReportAction
    public async Task<string> UpdateQuestionReportAction(QuizAndQuestionReportAction actionRequest)
    {
        QuestionIssueReport report = await _reportedQuestionRepository.GetAsync(q => q.Id == actionRequest.ReportId)
            ?? throw new AppException(Constants.NO_DATA_FOUND, StatusCodes.Status404NotFound);

        if (report.Severity == (int)QuestionOrQuizIssueReportSeverity.UnderProcessing)
        {
            throw new AppException(Constants.SEVERITY_UNDER_PROCESS_WARNING);
        }

        // Final states: Accepted or Ignored cannot be modified 
        if (report.Status == (int)QuestionOrQuizIssueReportStatus.Accepted ||
            report.Status == (int)QuestionOrQuizIssueReportStatus.Ignore)
        {
            throw new AppException(Constants.QUESTION_ISSUE_REPORT_FINALIZED_INFO);
        }

        // If UnderReview: only reviewer or superadmin can update 
        if (report.Status == (int)QuestionOrQuizIssueReportStatus.UnderReview
            && report.ModifiedBy != UserId
            && !string.Equals(UserRole, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(Constants.QUESTION_ISSUE_REPORT_NOT_HAVE_PERMISSION_EDIT);
        }

        // Update allowed
        report.Status = actionRequest.QuestionOrQuizIssueReportNewStatus;
        report.ModifiedBy = UserId;
        report.ModifiedDate = DateTime.UtcNow;

        await _reportedQuestionRepository.UpdateAsync(report);

        return Constants.QUESTION_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE;
    }
    #endregion

    #region GetQuestionIssueReportPreview
    public async Task<QuestionIssuePreviewRequestDto> GetQuestionIssueReportPreview(int queId)
    {
        QuestionIssuePreviewRequestDto response = new QuestionIssuePreviewRequestDto();

        response.QuestionDetail = await _questionPoolService.GetQuestionPreview(queId) ?? new QuestionDetailDTO();

        string query = string.Format(SqlConstants.GET_QUESTION_ISSUE_REPORT_PREVIEW_TEMPLATE, SqlConstants.GET_QUESTION_ISSUE_REPORT_PREVIEW_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_question_id", NpgsqlDbType.Integer) { Value = queId },
        };
        QuestionReportData reportData = await _sqlQueryRepository.SqlQuerySingleAsync<QuestionReportData>(query, parameters);

        response.ActiveBattleContainCount = reportData.ActiveBattleContainCount;
        response.ActiveQuizContainCount = reportData.ActiveQuizContainCount;

        return response;
    }
    #endregion

    #region GetAffectedQuizAndBattle
    public async Task<List<ActiveQuizBattleAffectedDTO>> GetAffectedQuizAndBattle(int queId)
    {
        string query = string.Format(SqlConstants.GET_QUESTION_ISSUE_REPORT_PREVIEW_TEMPLATE, SqlConstants.GET_AFFECTED_QUIZ_AND_BATTLE_LIST_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_question_id", NpgsqlDbType.Integer) { Value = queId },
        };
        return _mapper.Map<List<ActiveQuizBattleAffectedDTO>>
            (await _sqlQueryRepository.SqlQueryListAsync<ActiveQuizBattleAffectedDTO>(query, parameters));
    }
    #endregion

    #region Update Reported Question
    public async Task<string> UpdateReportedQuestion(int reportId, QuestionRequestDTO dto)
    {
        QuestionIssueReport report = await _reportedQuestionRepository.GetAsync(q => q.Id == reportId)
           ?? throw new AppException(Constants.NO_DATA_FOUND, StatusCodes.Status404NotFound);

        if (report.Severity == (int)QuestionOrQuizIssueReportSeverity.UnderProcessing)
        {
            throw new AppException(Constants.SEVERITY_UNDER_PROCESS_WARNING);
        }

        // Final states: Accepted or Ignored cannot be modified 
        if (report.Status == (int)QuestionOrQuizIssueReportStatus.Accepted ||
            report.Status == (int)QuestionOrQuizIssueReportStatus.Ignore)
        {
            throw new AppException(Constants.QUESTION_ISSUE_REPORT_FINALIZED_INFO);
        }

        // If UnderReview: only reviewer or superadmin can update 
        if (report.Status == (int)QuestionOrQuizIssueReportStatus.UnderReview
            && report.ModifiedBy != UserId
            && !string.Equals(UserRole, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(Constants.QUESTION_ISSUE_REPORT_NOT_HAVE_PERMISSION_EDIT);
        }

        try
        {
            var questionResponse = await _questionPoolService.CreateOrUpdateQuestion(report.QuestionId, dto);

            var request = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = (int)QuestionOrQuizIssueReportStatus.Accepted
            };

            var reportResponse = await UpdateQuestionReportAction(request);

            return $"{questionResponse}\n{reportResponse}";
        }
        catch (Exception ex)
        {
            QuizAndQuestionReportAction request = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = (int)QuestionOrQuizIssueReportStatus.Pending,
            };

            await UpdateQuestionReportAction(request);
            throw new AppException($"{ex.Message}\n{ Constants.REVERT_TO_PENDING_REPORT_QUESTION_STATUS}");
        }
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