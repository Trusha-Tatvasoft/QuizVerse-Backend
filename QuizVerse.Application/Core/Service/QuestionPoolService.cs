using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuestionPoolService(ISqlQueryRepository _sqlQueryRepository) : IQuestionPoolService
{
    public async Task<PageListResponse<QuestionPoolListDto>> GetQuestionPoolListAsync(PageListRequest pageListRequest)
    {
        string query = string.Format(SqlConstants.GET_QUESTION_POOL_LIST_QUERY_TEMPLATE, SqlConstants.GET_QUESTION_POOL_LIST_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_page_number", NpgsqlDbType.Integer) { Value = pageListRequest.PageNumber },
            new("p_page_size", NpgsqlDbType.Integer) { Value = pageListRequest.PageSize },
            new("p_search_term", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SearchTerm ?? DBNull.Value },
            new("p_sort_column", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SortColumn ?? DBNull.Value },
            new("p_sort_descending", NpgsqlDbType.Boolean) { Value = pageListRequest.SortDescending },
            new("p_category_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizCategoryId ?? DBNull.Value },
            new("p_difficulty_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuestionDifficultyId ?? DBNull.Value },
            new("p_question_type_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuestionTypeId ?? DBNull.Value },
        };

        string queryForTotalCount = string.Format(SqlConstants.GET_QUESTION_POOL_TOTAL_COUNT_QUERY_TEMPLATE, SqlConstants.GET_QUESTION_POOL_TOTAL_COUNT_FUNCTION);

        var parametersForTotalCount = new NpgsqlParameter[]
        {
            new("p_page_number", NpgsqlDbType.Integer) { Value = pageListRequest.PageNumber },
            new("p_page_size", NpgsqlDbType.Integer) { Value = pageListRequest.PageSize },
            new("p_search_term", NpgsqlDbType.Text) { Value = (object?)pageListRequest.SearchTerm ?? DBNull.Value },
            new("p_category_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizCategoryId ?? DBNull.Value },
            new("p_difficulty_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuestionDifficultyId ?? DBNull.Value },
            new("p_question_type_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuestionTypeId ?? DBNull.Value },
        };

        List<QuestionPoolListDto> questionPools = await _sqlQueryRepository.SqlQueryListAsync<QuestionPoolListDto>(query, parameters);
        TotalRecordsDto totalRecords = await _sqlQueryRepository.SqlQuerySingleAsync<TotalRecordsDto>(queryForTotalCount, parametersForTotalCount);

        PageListResponse<QuestionPoolListDto> response = new()
        {
            TotalRecords = totalRecords.TotalRecords,
            Records = questionPools
        };

        return response;
    }
}