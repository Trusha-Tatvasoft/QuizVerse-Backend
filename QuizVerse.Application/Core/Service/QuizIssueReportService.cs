using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Dynamic.Core;
namespace QuizVerse.Application.Core.Service;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class QuizReportIssueService(IGenericRepository<QuizIssueReport> _reportedQuizRepository, IHttpContextAccessor _httpContextAccessor,IMapper _mapper) : IQuizReportIssueService
{
    public string UserRole => _httpContextAccessor.HttpContext?.User?.GetUserRole() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public async Task<PageListResponse<QuizReportIssueResponseDTO>> GetQuizReportByPaginationAsync(PageListRequest query)
    {
        IQueryable<QuizIssueReport> quizIssueReportsQuery = _reportedQuizRepository
            .GetQueryableInclude(q => q.Quiz, q => q.User, q => q.Quiz.CreatedByNavigation);

        // Sorting logic (same as before)
        if (!string.IsNullOrEmpty(query.SortColumn))
        {
            switch (query.SortColumn.ToLower())
            {
                case "quiz":
                    query.SortColumn = "Quiz.Name";
                    break;
                case "creator":
                    query.SortColumn = "Quiz.CreatedByNavigation.FullName";
                    break;
                case "reporter":
                    query.SortColumn = "User.FullName";
                    break;
                case "severity":
                    query.SortColumn = "Severity";
                    break;
                default:
                    query.SortColumn = "Id";
                    break;
            }

            quizIssueReportsQuery = quizIssueReportsQuery
                .OrderBy($"{query.SortColumn} {(query.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            quizIssueReportsQuery = quizIssueReportsQuery.OrderBy("Id asc");
        }

        List<QuizReportIssueResponseDTO>? mappedResult = _mapper.Map<List<QuizReportIssueResponseDTO>>(await quizIssueReportsQuery.ToListAsync());
        int TotalRecords = mappedResult.Count;

        PageListResponse<QuizReportIssueResponseDTO> pageListResponse = new()
        {
            TotalRecords = TotalRecords,
            Records = [.. mappedResult
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)]
        };

        return pageListResponse;
    }
}