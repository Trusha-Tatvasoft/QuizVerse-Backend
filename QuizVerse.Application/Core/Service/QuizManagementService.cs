using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuizVerse.Application.Core.Service;

public class QuizManagementService(
        IGenericRepository<Quiz> quizRepository,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        ISqlQueryRepository _sqlQueryRepository
) : IQuizManagementService
{
    public int? UserId => httpContextAccessor.HttpContext?.User?.GetUserId();

    // #region Get Card Data
    // public async Task<QuizManagementPageDataDto> GetQuizCardData()
    // {
    //     var data = await quizRepository
    //         .GetQueryableInclude(q => q.QuizAttempteds, q => q.QuizToBaseQuestionMaps)
    //         .Where(q => !q.IsDeleted)
    //         .Select(q => new
    //         {
    //             IsActive = q.Status == (int)QuizStatus.Active,
    //             Participants = q.QuizAttempteds.Select(qa => qa.UserId),
    //             Questions = q.QuizToBaseQuestionMaps.Select(qm => qm.QueId)
    //         })
    //         .ToListAsync();

    //     long totalQuiz = data.Count;
    //     long activeQuiz = data.Count(q => q.IsActive);
    //     long totalParticipants = data.SelectMany(q => q.Participants).Distinct().Count();
    //     long totalQuestions = data.SelectMany(q => q.Questions).Distinct().Count();

    //     return new QuizManagementPageDataDto
    //     {
    //         TotalQuiz = totalQuiz,
    //         ActiveQuiz = activeQuiz,
    //         TotalParticipants = totalParticipants,
    //         TotalQuestions = totalQuestions
    //     };
    // }
    // #endregion

    // #region Get Quiz List
    // public async Task<PageListResponse<QuizListDto>> GetQuizzesByPagination(PageListRequest pageListRequest)
    // {
    //     var query = quizRepository
    //         .GetQueryableInclude(q => q.Category, q => q.DifficultyLevel)
    //         .Where(q => !q.IsDeleted);

    //     // Search
    //     if (!string.IsNullOrWhiteSpace(pageListRequest.SearchTerm))
    //     {
    //         var term = pageListRequest.SearchTerm.ToLower();
    //         query = query.Where(q =>
    //             q.Name.ToLower().Contains(term) ||
    //             q.Category.CategoryName.ToLower().Contains(term));
    //     }

    //     // Filters
    //     var filters = pageListRequest.Filters;
    //     if (filters != null)
    //     {
    //         if (filters.QuizStatus.HasValue)
    //         {
    //             if (!Enum.IsDefined(typeof(QuizStatus), filters.QuizStatus.Value))
    //                 throw new AppException(Constants.INVALID_QUIZ_STATUS_MESSAGE);

    //             query = query.Where(q => q.Status == (int)filters.QuizStatus.Value);
    //         }

    //         if (filters.QuizCategoryId.HasValue)
    //             query = query.Where(q => q.CategoryId == filters.QuizCategoryId.Value);

    //         if (filters.QuizDifficultyId.HasValue)
    //             query = query.Where(q => q.DifficultyLevelId == filters.QuizDifficultyId.Value);
    //     }

    //     // Sorting (special mapping for category & difficulty)
    //     if (!string.IsNullOrWhiteSpace(pageListRequest.SortColumn))
    //     {
    //         string sortColumn = pageListRequest.SortColumn;

    //         if (sortColumn.Equals("category", StringComparison.OrdinalIgnoreCase))
    //             sortColumn = "Category.CategoryName";
    //         else if (sortColumn.Equals("difficulty", StringComparison.OrdinalIgnoreCase))
    //             sortColumn = "DifficultyLevel.Name";

    //         query = query.OrderBy($"{sortColumn} {(pageListRequest.SortDescending ? "desc" : "asc")}");
    //     }
    //     else
    //     {
    //         query = query.OrderBy("Id asc");
    //     }

    //     return await quizRepository.PaginatedList<QuizListDto>(query, pageListRequest, q => q.ProjectTo<QuizListDto>(mapper.ConfigurationProvider));
    // }
    // #endregion

    #region update refrence
    public async Task MoveQuizzesToCategoryAsync(QuizCategory quizCategoryWithQuizzes, int toCategoryId)
    {
        if (quizCategoryWithQuizzes is not null && quizCategoryWithQuizzes.Quizzes is not null)
        {
            foreach (var quiz in quizCategoryWithQuizzes.Quizzes)
            {
                quiz.CategoryId = toCategoryId;
                await quizRepository.UpdateAsync(quiz);
            }
        }
    }
    #endregion

    #region Create/Update Quiz
    public async Task<CreateUpdateResponseDto> CreateUpdateQuiz(SaveQuizRequestDto quizCreateUpdateRequestDto)
    {
        if (quizCreateUpdateRequestDto == null)
            throw new AppException(Constants.INVALID_DATA_MESSAGE);

        string query = string.Format(
            SqlConstants.CREATE_UPDATE_QUIZ_QUERY_TEMPLATE,
            SqlConstants.CREATE_UPDATE_QUIZ_FUNCTION
        );

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var parameters = new NpgsqlParameter[]
        {
            new("p_quiz_id", NpgsqlDbType.Integer) { Value = quizCreateUpdateRequestDto.Id ?? (object)DBNull.Value },
            new("p_name", NpgsqlDbType.Text) { Value = quizCreateUpdateRequestDto.Name },
            new("p_category_id", NpgsqlDbType.Integer) { Value = quizCreateUpdateRequestDto.CategoryId },
            new("p_description", NpgsqlDbType.Text) { Value = quizCreateUpdateRequestDto.Description },
            new("p_total_time", NpgsqlDbType.Integer) { Value = quizCreateUpdateRequestDto.TotalTime },
            new("p_difficulty_level_id", NpgsqlDbType.Integer) { Value = quizCreateUpdateRequestDto.DifficultyLevelId },
            new("p_total_question", NpgsqlDbType.Integer) { Value = quizCreateUpdateRequestDto.TotalQuestion },
            new("p_is_paid", NpgsqlDbType.Boolean) { Value = quizCreateUpdateRequestDto.IsPaid },
            new("p_price", NpgsqlDbType.Numeric) { Value = (object?)quizCreateUpdateRequestDto.Price ?? DBNull.Value },
            new("p_status", NpgsqlDbType.Integer) { Value = quizCreateUpdateRequestDto.Status },
            new("p_tags", NpgsqlDbType.Jsonb)
            {
                Value = quizCreateUpdateRequestDto.Tags != null
                    ? JsonSerializer.Serialize(quizCreateUpdateRequestDto.Tags, jsonOptions)
                    : "[]"
            },
            new("p_questions", NpgsqlDbType.Jsonb)
            {
                Value = quizCreateUpdateRequestDto.Questions != null
                    ? JsonSerializer.Serialize(quizCreateUpdateRequestDto.Questions, jsonOptions)
                    : "[]"
            },
            new("p_created_by", NpgsqlDbType.Integer) { Value = UserId ?? (object)DBNull.Value },
        };

        return await _sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);
    }
    #endregion

    #region Get Quiz Data By Id
    public async Task<QuizResponseDto> GetQuizDataById(int quizId)
    {
        if (quizId <= 0)
            throw new AppException(Constants.INVALID_DATA_MESSAGE);

        string query = string.Format(
            SqlConstants.GET_QUIZ_DATA_BY_ID_QUERY_TEMPLATE,
            SqlConstants.GET_QUIZ_DATA_BY_ID_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_quiz_id", NpgsqlDbType.Integer) { Value = quizId }
        };

        return await _sqlQueryRepository.SqlQuerySingleAsync<QuizResponseDto>(query, parameters);
    }
    #endregion
}
