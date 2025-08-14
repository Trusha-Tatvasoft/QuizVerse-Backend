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
using QuizVerse.Infrastructure.Interface;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Net;

namespace QuizVerse.Application.Core.Service;

public class QuizManagementService(
        IGenericRepository<Quiz> quizRepository,
        IGenericRepository<QuestionType> questionTypeRepository,
        IGenericRepository<QuestionDifficulty> questionDifficultyRepository,
        IGenericRepository<QuizCategory> quizCatgoryRepository,
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

    #region Delete Quiz
    public async Task<CreateUpdateResponseDto> DeleteQuiz(int quizId)
    {
        if (quizId <= 0)
            throw new AppException(Constants.INVALID_DATA_MESSAGE);

        string query = string.Format(
            SqlConstants.DELETE_QUIZ_QUERY_TEMPLATE,
            SqlConstants.DELETE_QUIZ_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_quiz_id", NpgsqlDbType.Integer) { Value = quizId },
            new("p_modified_by", NpgsqlDbType.Integer) { Value = UserId ?? (object)DBNull.Value },
        };
        return await _sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);
    }
    #endregion

    #region Export Quiz Questions to CSV
    public async Task<string> ExportQuestionsToCsv(List<QuestionsListRequestDto> questions)
    {
        if (questions == null || !questions.Any())
            throw new AppException("No questions provided", 400);

        var csvRows = new List<QuestionCsvRow>();

        foreach (var q in questions)
        {
            // Validate Question Text
            if (string.IsNullOrWhiteSpace(q.QueText))
                throw new AppException("Question text is missing", 400);

            // Get type/difficulty/category names
            var typeName = await GetQuestionTypeName(q.QueTypeId);
            var difficultyName = await GetDifficultyName(q.QueDifficultyId);
            var categoryName = await GetCategoryName(q.CategoryId);

            if (string.IsNullOrWhiteSpace(typeName))
                throw new AppException($"Invalid Question Type ID: {q.QueTypeId}", 400);

            if (string.IsNullOrWhiteSpace(difficultyName))
                throw new AppException($"Invalid Difficulty ID: {q.QueDifficultyId}", 400);

            if (string.IsNullOrWhiteSpace(categoryName))
                throw new AppException($"Invalid Category ID: {q.CategoryId}", 400);

            // Validate options and answer for Multiple Choice & True/False
            if (typeName.Equals("Multiple Choice", StringComparison.OrdinalIgnoreCase))
            {
                if (q.QueOptionsAns == null || q.QueOptionsAns.Count(o => o.Key.StartsWith("option", StringComparison.OrdinalIgnoreCase)) < 2)
                    throw new AppException("Multiple choice question must have at least 2 options", 400);

                if (!q.QueOptionsAns.Any(o => o.Key.Equals("answer", StringComparison.OrdinalIgnoreCase)))
                    throw new AppException("Correct answer is missing for multiple choice question", 400);
            }
            else if (typeName.Equals("True/False", StringComparison.OrdinalIgnoreCase))
            {
                if (!q.QueOptionsAns?.Any(o => o.Key.Equals("answer", StringComparison.OrdinalIgnoreCase)) ?? true)
                    throw new AppException("Correct answer is missing for true/false question", 400);
            }
            else if (typeName.Equals("Short Answer", StringComparison.OrdinalIgnoreCase))
            {
                if (!q.QueOptionsAns?.Any(o => o.Key.Equals("answer", StringComparison.OrdinalIgnoreCase)) ?? true)
                    throw new AppException("Correct answer is missing for short answer question", 400);
            }

            // Build CSV row
            var row = new QuestionCsvRow
            {
                Question = q.QueText,
                Type = typeName,
                Difficulty = difficultyName,
                Category = categoryName
            };

            // Extract options & correct answer (fills missing with "")
            if (q.QueOptionsAns != null && q.QueOptionsAns.Any())
            {
                var options = q.QueOptionsAns
                    .Where(o => o.Key.StartsWith("option", StringComparison.OrdinalIgnoreCase))
                    .Select(o => o.Value)
                    .Take(4)
                    .ToList();

                while (options.Count < 4)
                    options.Add(string.Empty);

                row.Option1 = options.ElementAtOrDefault(0) ?? string.Empty;
                row.Option2 = options.ElementAtOrDefault(1) ?? string.Empty;
                row.Option3 = options.ElementAtOrDefault(2) ?? string.Empty;
                row.Option4 = options.ElementAtOrDefault(3) ?? string.Empty;

                row.CorrectAnswer = q.QueOptionsAns
                    .FirstOrDefault(o => o.Key.Equals("answer", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
            }

            csvRows.Add(row);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, config);
        csv.WriteHeader<QuestionCsvRow>();
        csv.NextRecord();
        csv.WriteRecords(csvRows);

        return writer.ToString();
    }

    private async Task<string> GetQuestionTypeName(int typeId)
    {
        QuestionType? type = await questionTypeRepository.GetAsync(qt => qt.Id == typeId);
        return type?.TypeName ?? string.Empty;
    }

    private async Task<string> GetDifficultyName(int difficultyId)
    {
        QuestionDifficulty? questionDifficulty = await questionDifficultyRepository.GetAsync(qd => qd.Id == difficultyId);
        return questionDifficulty?.Name ?? string.Empty;
    }

    private async Task<string> GetCategoryName(int categoryId)
    {
        QuizCategory? quizCategory = await quizCatgoryRepository.GetAsync(qc => qc.Id == categoryId);
        return quizCategory?.CategoryName ?? string.Empty;
    }
    #endregion
}
