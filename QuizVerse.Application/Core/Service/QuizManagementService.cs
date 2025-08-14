using AutoMapper;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizManagementService(
        IGenericRepository<Quiz> quizRepository,
        IMapper mapper, ISqlQueryRepository _sqlQueryRepository
) : IQuizManagementService
{
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
}
