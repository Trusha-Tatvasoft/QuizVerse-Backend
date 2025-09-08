using AutoMapper;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class BrowseQuizzesService(ISqlQueryRepository _sqlQueryRepository, IMapper _mapper) : IBrowseQuizzesService
{
    public async Task<BrowseQuizzesResponseDTO> BrowseQuizzes(BrowseQuizzesRequestDTO request)
    {
        // Set default values for optional parameters and validations
        if (request.BrowseQuizzesSorting == 0 || request.BrowseQuizzesSorting == null) request.BrowseQuizzesSorting = BrowseQuizzesSorting.Newest;
        if (request.SearchText == null) request.SearchText = "";

        if (request.FilterRanges is not null)
        {
            if (request.FilterRanges.MinPrice.HasValue && request.FilterRanges.MaxPrice.HasValue &&
                request.FilterRanges.MinPrice > request.FilterRanges.MaxPrice)
            {
                throw new ArgumentException(Constants.MIN_PRICE_LESS_THAN_MAX_PRICE);
            }

            if (request.FilterRanges.MinRating.HasValue && request.FilterRanges.MaxRating.HasValue &&
                request.FilterRanges.MinRating > request.FilterRanges.MaxRating)
            {
                throw new ArgumentException(Constants.MIN_RATING_LESS_THAN_MAX_RATING);
            }

            if (request.FilterRanges.MinTotalTime.HasValue && request.FilterRanges.MaxTotalTime.HasValue &&
                request.FilterRanges.MinTotalTime > request.FilterRanges.MaxTotalTime)
            {
                throw new ArgumentException(Constants.MIN_TOTAL_TIME_LESS_THAN_MAX_TOTAL_TIME);
            }
        }

        string query = string.Format(SqlConstants.Browse_Quizzes_QUERY_TEMPLATE, SqlConstants.Browse_Quizzes_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_search_text", NpgsqlDbType.Text) { Value = (object?)request.SearchText ?? "" },
            new("p_quiz_category_id", NpgsqlDbType.Integer) { Value = (object?)request.QuizCategoryId ?? DBNull.Value },
            new("p_quiz_difficulty_level_id", NpgsqlDbType.Integer) { Value = (object?)request.QuizDifficultyLevelId ?? DBNull.Value },
            new("p_tag_ids", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = (object?)request.TagIds ?? DBNull.Value },
            new("p_sort_by", NpgsqlDbType.Text) { Value = (object?)request.BrowseQuizzesSorting.ToString() ?? DBNull.Value },
            new("p_filter_by_type", NpgsqlDbType.Text) {   Value = string.IsNullOrEmpty(request.BrowseQuizzesFilterByType?.ToString())
            ? DBNull.Value
            : request.BrowseQuizzesFilterByType.ToString()  },
            new("p_batch_number", NpgsqlDbType.Integer) { Value = request.BatchNumber },
            new("p_min_price", NpgsqlDbType.Numeric) { Value = (object?)request.FilterRanges?.MinPrice ?? 0 },
            new("p_max_price", NpgsqlDbType.Numeric) { Value = (object?)request.FilterRanges?.MaxPrice ?? DBNull.Value },
            new("p_min_rating", NpgsqlDbType.Numeric) { Value = (object?)request.FilterRanges?.MinRating ?? 0 },
            new("p_max_rating", NpgsqlDbType.Numeric) { Value = (object?)request.FilterRanges?.MaxRating ?? Constants.MAX_RATING },
            new("p_min_total_time", NpgsqlDbType.Numeric) { Value = (object?)request.FilterRanges?.MinTotalTime ?? Constants.MIN_QUIZ_TOTAL_TIME_MINUTES },
            new("p_max_total_time", NpgsqlDbType.Numeric) { Value = (object?)request.FilterRanges?.MaxTotalTime ?? Constants.MAX_QUIZ_TOTAL_TIME_MINUTES }
        };

        // Execute the query and get the result as JSON plues has_more flag
        BrowseQuizzesResultDTO result = await _sqlQueryRepository.SqlQuerySingleAsync<BrowseQuizzesResultDTO>(query, parameters);

        // Deserialize the JSON result to List<BrowseQuizz> and prepare the response DTO
        BrowseQuizzesResponseDTO response = _mapper.Map<BrowseQuizzesResponseDTO>(result);

        return response;
    }
}
