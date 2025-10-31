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

public class ContentModerationService(IGenericRepository<QuizIssueReport> _reportedQuizRepository, IHttpContextAccessor _httpContextAccessor, IMapper _mapper) : IContentModerationService
{
    public string UserRole => _httpContextAccessor.HttpContext?.User?.GetUserRole() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
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
}