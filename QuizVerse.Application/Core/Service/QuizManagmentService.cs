using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Dynamic.Core;

namespace QuizVerse.Application.Core.Service;

public class QuizManagmentService(
        IGenericRepository<Quiz> quizRepository,
        IMapper mapper
) : IQuizManagmentService
{
    #region Get Card Data
    public async Task<QuizManagmentPageDataDto> GetQuizCardData()
    {
        var data = await quizRepository
            .GetQueryableInclude(q => q.QuizAttempteds, q => q.QuizToBaseQuestionMaps)
            .Where(q => !q.IsDeleted)
            .Select(q => new
            {
                IsActive = q.Status == 1,
                Participants = q.QuizAttempteds.Select(qa => qa.UserId),
                Questions = q.QuizToBaseQuestionMaps.Select(qm => qm.QueId)
            })
            .ToListAsync();

        long totalQuiz = data.Count;
        long activeQuiz = data.Count(q => q.IsActive);
        long totalParticipants = data.SelectMany(q => q.Participants).Distinct().Count();
        long totalQuestions = data.SelectMany(q => q.Questions).Distinct().Count();

        return new QuizManagmentPageDataDto
        {
            TotalQuiz = totalQuiz,
            ActiveQuiz = activeQuiz,
            TotalParticipants = totalParticipants,
            TotalQuestions = totalQuestions
        };
    }
    #endregion
    
    #region Get Quiz List
    public async Task<PageListResponse<QuizListDto>> GetQuizzesByPagination(PageListRequest pageListRequest)
    {
        var query = quizRepository
            .GetQueryableInclude(q => q.Category, q => q.DifficultyLevel)
            .Where(q => !q.IsDeleted);

        // Search
        if (!string.IsNullOrWhiteSpace(pageListRequest.SearchTerm))
        {
            var term = pageListRequest.SearchTerm.ToLower();
            query = query.Where(q =>
                q.Name.ToLower().Contains(term) ||
                q.Category.CategoryName.ToLower().Contains(term));
        }

        // Filters
        var filters = pageListRequest.Filters;
        if (filters != null)
        {
            if (filters.QuizStatus.HasValue)
            {
                if (!Enum.IsDefined(typeof(QuizStatus), filters.QuizStatus.Value))
                    throw new AppException(Constants.INVALID_QUIZ_STATUS_MESSAGE);

                query = query.Where(q => q.Status == (int)filters.QuizStatus.Value);
            }

            if (filters.QuizCategoryId.HasValue)
                query = query.Where(q => q.CategoryId == filters.QuizCategoryId.Value);

            if (filters.QuizDifficultyId.HasValue)
                query = query.Where(q => q.DifficultyLevelId == filters.QuizDifficultyId.Value);
        }

        // Sorting (special mapping for category & difficulty)
        if (!string.IsNullOrWhiteSpace(pageListRequest.SortColumn))
        {
            string sortColumn = pageListRequest.SortColumn;

            if (sortColumn.Equals("category", StringComparison.OrdinalIgnoreCase))
                sortColumn = "Category.CategoryName";
            else if (sortColumn.Equals("difficulty", StringComparison.OrdinalIgnoreCase))
                sortColumn = "DifficultyLevel.Name";

            query = query.OrderBy($"{sortColumn} {(pageListRequest.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            query = query.OrderBy("Id asc");
        }

        return await quizRepository.PaginatedList<QuizListDto>(query, pageListRequest, q => q.ProjectTo<QuizListDto>(mapper.ConfigurationProvider));
    }
    #endregion
}
