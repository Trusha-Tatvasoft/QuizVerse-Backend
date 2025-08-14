using AutoMapper;
using Microsoft.AspNetCore.Http;
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

    #region Get Card Data
    public async Task<QuizManagementPageDataDto> GetQuizCardData()
    {
        var parameters = new NpgsqlParameter[]
        {
            new("p_active_status", NpgsqlDbType.Integer) { Value = (int)QuizStatus.Active }
        };

        string query = string.Format(
            SqlConstants.GET_QUIZ_CARD_DATA_QUERY_TEMPLATE,
            SqlConstants.GET_QUIZ_CARD_DATA_FUNCTION);

        var result = await _sqlQueryRepository.SqlQuerySingleAsync<QuizManagementPageDataDto>(query, parameters);

        return result ?? new QuizManagementPageDataDto();
    }
    #endregion

    #region Get Quiz List
    public async Task<PageListResponse<QuizListDto>> GetQuizzesByPagination(PageListRequest pageListRequest)
    {
        // --- LIST QUERY ---
        string listQuery = string.Format(
            SqlConstants.GET_QUIZ_LIST_QUERY_TEMPLATE,
            SqlConstants.GET_QUIZ_LIST_FUNCTION
        );

        var listParameters = new NpgsqlParameter[]
        {
            new("p_page_number", NpgsqlDbType.Integer) { Value = pageListRequest.PageNumber },
            new("p_page_size", NpgsqlDbType.Integer) { Value = pageListRequest.PageSize },
            new("p_search_term", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SearchTerm ?? DBNull.Value },
            new("p_sort_column", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SortColumn ?? DBNull.Value },
            new("p_sort_descending", NpgsqlDbType.Boolean) { Value = pageListRequest.SortDescending },
            new("p_quiz_status", NpgsqlDbType.Integer) {  Value = pageListRequest.Filters?.QuizStatus != null? (int)pageListRequest.Filters.QuizStatus: DBNull.Value},
            new("p_category_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizCategoryId ?? DBNull.Value },
            new("p_difficulty_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizDifficultyId ?? DBNull.Value }
        };

        List<QuizListDto> quizzes = await _sqlQueryRepository.SqlQueryListAsync<QuizListDto>(
            listQuery, listParameters
        );

        // --- COUNT QUERY ---
        string countQuery = string.Format(
            SqlConstants.GET_QUIZ_LIST_COUNT_QUERY_TEMPLATE, 
            SqlConstants.GET_QUIZ_LIST_COUNT_FUNCTION
        );

        var countParameters = new NpgsqlParameter[]
        {
            new("p_search_term", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SearchTerm ?? DBNull.Value },
            new("p_quiz_status", NpgsqlDbType.Integer) {  Value = pageListRequest.Filters?.QuizStatus != null? (int)pageListRequest.Filters.QuizStatus: DBNull.Value},
            new("p_category_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizCategoryId ?? DBNull.Value },
            new("p_difficulty_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizDifficultyId ?? DBNull.Value },
        };

        TotalRecordsDto totalRecordsStr = await _sqlQueryRepository.SqlQuerySingleAsync<TotalRecordsDto>(
            countQuery, countParameters
        );

        return new PageListResponse<QuizListDto>
        {
            TotalRecords = totalRecordsStr.TotalRecords,
            Records = mapper.Map<List<QuizListDto>>(quizzes)
        };
    }
    #endregion

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