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
namespace QuizVerse.Application.Core.Service;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Enums;

public class ContentModerationService(IGenericRepository<QuizIssueReport> _reportedQuizRepository,IGenericRepository<Quiz> _quizRepository, IHttpContextAccessor _httpContextAccessor, IMapper _mapper) : IContentModerationService
{
    public int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public string UserRole => _httpContextAccessor.HttpContext?.User?.GetUserRole() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    #region GetQuizReportByPaginationAsync
    private IQueryable<QuizIssueReport> GetQuizReportData(PageListRequest query)
    {
        IQueryable<QuizIssueReport> quizIssueReportsQuery = _reportedQuizRepository
            .GetQueryableInclude(q => q.Quiz, q => q.User, q => q.Quiz.CreatedByNavigation, q => q.ModifiedByNavigation);

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

    #region QuizReportAction
    public async Task<string> UpdateQuizReportAction(QuizAndQuestionReportAction actionRequest)
    {
        QuizIssueReport report = await _reportedQuizRepository.GetAsync(q => q.Id == actionRequest.ReportId)
            ?? throw new AppException(Constants.NO_DATA_FOUND, StatusCodes.Status404NotFound);

        // Final states: Accepted or Ignored cannot be modified 
        if (report.Status == (int)QuestionOrQuizIssueReportStatus.Accepted ||
            report.Status == (int)QuestionOrQuizIssueReportStatus.Ignore)
        {
            throw new AppException(Constants.QUIZ_ISSUE_REPORT_FINALIZED_INFO);
        }

        // If status is 2 → Inactive quiz
        if (actionRequest.QuestionOrQuizIssueReportNewStatus == (int)QuestionOrQuizIssueReportStatus.Accepted)
        {
            var quiz = await _reportedQuizRepository
                .GetQueryableInclude(u => u.Quiz)
                .Where(u => u.Id == actionRequest.ReportId)
                .Select(u => u.Quiz)
                .FirstOrDefaultAsync();

            if (quiz != null)
            {
                quiz.Status = (int)QuizStatus.Inactive;
                quiz.ModifiedBy = UserId;
                quiz.ModifiedDate = DateTime.UtcNow;
                await _quizRepository.UpdateAsync(quiz);
            }
        }

        // If UnderReview: only reviewer or superadmin can update 
        if (report.Status == (int)QuestionOrQuizIssueReportStatus.UnderReview
            && report.ModifiedBy != UserId
            && !string.Equals(UserRole, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(Constants.QUIZ_ISSUE_REPORT_NOT_HAVE_PERMISSION_EDIT);
        }

        // Update allowed
        report.Status = actionRequest.QuestionOrQuizIssueReportNewStatus;
        report.ModifiedBy = UserId;
        report.ModifiedDate = DateTime.UtcNow;

        await _reportedQuizRepository.UpdateAsync(report);

        return Constants.QUIZ_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE;
    }
    #endregion
}