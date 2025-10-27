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
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Service;

public class QuizManagementService(
        IGenericRepository<Quiz> quizRepository,
        IGenericRepository<QuestionType> questionTypeRepository,
        IGenericRepository<QuestionDifficulty> questionDifficultyRepository,
        IGenericRepository<QuizCategory> quizCatgoryRepository,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        ISqlQueryRepository _sqlQueryRepository,
        IDropDownDataService dropDownDataService,
        ICommonService commonService
) : IQuizManagementService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region Get Card Data
    public async Task<QuizManagementPageDataDto> GetQuizCardData()
    {
        var parameters = new NpgsqlParameter[]
        {
            new("p_active_status", NpgsqlDbType.Integer) { Value = (int)QuizStatus.Active },
            new("p_quiz_type", NpgsqlDbType.Integer) { Value = (int)QuizType.Normal }
        };

        string query = string.Format(
            SqlConstants.GET_QUIZ_CARD_DATA_QUERY_TEMPLATE,
            SqlConstants.GET_QUIZ_CARD_DATA_FUNCTION);

        return await _sqlQueryRepository.SqlQuerySingleAsync<QuizManagementPageDataDto>(query, parameters);
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
            new("p_difficulty_id", NpgsqlDbType.Integer) { Value = (object?)pageListRequest.Filters?.QuizDifficultyId ?? DBNull.Value },
            new("p_quiz_type", NpgsqlDbType.Integer) { Value = (int)QuizType.Normal }
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
            new("p_quiz_type", NpgsqlDbType.Integer) { Value = (int)QuizType.Normal }
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
            new("p_total_time", NpgsqlDbType.Numeric) { Value = quizCreateUpdateRequestDto.TotalTime },
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
            new("p_no_of_questions_per_difficulty", NpgsqlDbType.Jsonb)
            {
                Value = quizCreateUpdateRequestDto.NoOfQuestionsPerDifficulty != null
                    ? JsonSerializer.Serialize(quizCreateUpdateRequestDto.NoOfQuestionsPerDifficulty, jsonOptions)
                    : "[]"
            },
            new("p_created_by", NpgsqlDbType.Integer) { Value = UserId },
        };

        CreateUpdateResponseDto response = await _sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);

        if (response == null)
            throw new AppException(Constants.CREATE_OR_UPDATE_QUIZ_FAILED, 500);

        if (!response.Success)
            throw new AppException(response.Message, 400);
        else
            dropDownDataService.ClearCache(DropDownType.QuizTag);


        return response;
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
    public async Task<CreateUpdateResponseDto> UpdateQuizAction(QuizActionDataDto actionDto)
    {
        string query = string.Format(
            SqlConstants.UPDATE_QUIZ_ACTION_QUERY_TEMPLATE,
            SqlConstants.UPDATE_QUIZ_ACTION_QUERY_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_quiz_id", NpgsqlDbType.Integer) { Value = actionDto.Id },
            new("p_is_deleted_action", NpgsqlDbType.Boolean ) {Value = actionDto.Action == UserActionType.Delete},
            new("p_is_active_status", NpgsqlDbType.Boolean ) { Value = actionDto.NewStatus.HasValue ? ((int)actionDto.NewStatus.Value == (int)QuizStatus.Active) : DBNull.Value },
            new("p_modified_by", NpgsqlDbType.Integer) { Value = UserId },
        };

        CreateUpdateResponseDto response = await _sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);

        if (response == null)
            throw new AppException(Constants.DELETE_QUIZ_FAILED, 500);

        if (!response.Success)
            throw new AppException(response.Message, 400);

        return response;
    }
    #endregion

    #region Export Quiz Questions to CSV
    public async Task<string> ExportQuestionsToCsv(ExportQuizQuestionsRequestDto exportRequest)
    {
        if (string.IsNullOrWhiteSpace(exportRequest.QuizName))
        {
            throw new AppException(Constants.INVALID_EXPORT_REQUEST_QUIZNAME, 400);
        }


        if (exportRequest.Questions == null || !exportRequest.Questions.Any())
        {
            throw new AppException(Constants.INVALID_EXPORT_REQUEST_QUESTIONS, 400);
        }

        List<QuestionsListRequestDto>? questions = exportRequest.Questions;

        List<int> questionTypeIds = questions.Select(q => q.QueTypeId).Distinct().ToList();
        List<int> questionDifficultyIds = questions.Select(q => q.QueDifficultyId).Distinct().ToList();
        List<int> categoryIds = questions.Select(q => q.CategoryId).Distinct().ToList();

        // Fetch question types, difficulties, and categories
        List<QuestionType> questionTypes = questionTypeRepository.GetQueryableInclude()
            .Where(qt => questionTypeIds.Contains(qt.Id))
            .ToList();

        List<QuestionDifficulty> questionDifficulties = questionDifficultyRepository.GetQueryableInclude()
            .Where(qd => questionDifficultyIds.Contains(qd.Id) && !qd.IsDeleted)
            .ToList();

        List<QuizCategory> quizCategories = quizCatgoryRepository.GetQueryableInclude()
            .Where(qc => categoryIds.Contains(qc.Id) && !qc.IsDeleted)
            .ToList();

        // Create a memory stream for CSV output
        using (var memoryStream = new MemoryStream())
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
        {
            // Write CSV headers
            await streamWriter.WriteLineAsync(Constants.EXPORT_QUESTIONS_CSV_HEADER);

            foreach (var q in questions)
            {
                // Validate Question Text
                if (string.IsNullOrWhiteSpace(q.QueText))
                    throw new AppException(Constants.MISSING_QUESTION_TEXT, 400);

                // Get type/difficulty/category names
                var typeName = questionTypes.Find(t => t.Id == q.QueTypeId)?.TypeName;
                var difficultyName = questionDifficulties.Find(d => d.Id == q.QueDifficultyId)?.Name;
                var categoryName = quizCategories.Find(c => c.Id == q.CategoryId)?.CategoryName;

                if (string.IsNullOrWhiteSpace(typeName))
                    throw new AppException(string.Format(Constants.INVALID_QUESTION_TYPE_ID, q.QueTypeId), 400);

                if (string.IsNullOrWhiteSpace(difficultyName))
                    throw new AppException(string.Format(Constants.INVALID_QUESTION_DIFFICULTY_ID, q.QueDifficultyId), 400);

                if (string.IsNullOrWhiteSpace(categoryName))
                    throw new AppException(string.Format(Constants.INVALID_CATEGORY_ID, q.CategoryId), 400);

                // Validate options and answer
                if (typeName.Equals(Constants.QUESTION_TYPE_MULTIPLE_CHOICE, StringComparison.OrdinalIgnoreCase))
                {
                    if (q.QueOptionsAns == null || q.QueOptionsAns.Count(o => o.Key.StartsWith(Constants.QUESTION_KEY_OPTION, StringComparison.OrdinalIgnoreCase)) < 2)
                        throw new AppException(Constants.INVALID_MCQ_OPTIONS, 400);

                    if (!q.QueOptionsAns.Any(o => o.Key.Equals(Constants.QUESTION_KEY_ANSWER, StringComparison.OrdinalIgnoreCase)))
                        throw new AppException(string.Format(Constants.NO_CORRECT_ANSWER, typeName.ToLower()), 400);
                }
                else if (typeName.Equals(Constants.QUESTION_TYPE_TRUE_FALSE, StringComparison.OrdinalIgnoreCase) ||
                        typeName.Equals(Constants.QUESTION_TYPE_SHORT_ANSWER, StringComparison.OrdinalIgnoreCase) ||
                         typeName.Equals(Constants.QUESTION_TYPE_FILL_IN_THE_BLANKS, StringComparison.OrdinalIgnoreCase))
                {
                    if (!q.QueOptionsAns?.Any(o => o.Key.Equals(Constants.QUESTION_KEY_ANSWER, StringComparison.OrdinalIgnoreCase)) ?? true)
                        throw new AppException(string.Format(Constants.NO_CORRECT_ANSWER, typeName.ToLower()), 400);
                }

                // Build CSV line
                var csvLine = new StringBuilder();

                // Add core fields
                csvLine.Append(commonService.EscapeCsv(q.QueText)).Append(",");
                csvLine.Append(commonService.EscapeCsv(typeName)).Append(",");
                csvLine.Append(commonService.EscapeCsv(difficultyName)).Append(",");
                csvLine.Append(commonService.EscapeCsv(categoryName)).Append(",");

                // Add options (up to 4)
                var options = q.QueOptionsAns?
                    .Where(o => o.Key.StartsWith(Constants.QUESTION_KEY_OPTION, StringComparison.OrdinalIgnoreCase))
                    .Select(o => commonService.EscapeCsv(o.Value))
                    .Take(4)
                    .ToList() ?? new List<string>();

                // Pad with empty strings if needed
                while (options.Count < 4)
                    options.Add(string.Empty);

                csvLine.Append(string.Join(",", options)).Append(",");

                // Add correct answer
                var correctAnswer = q.QueOptionsAns?
                    .FirstOrDefault(o => o.Key.Equals(Constants.QUESTION_KEY_ANSWER, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
                csvLine.Append(commonService.EscapeCsv(correctAnswer));

                // Write the line
                await streamWriter.WriteLineAsync(csvLine.ToString());
            }

            // Return the CSV content
            streamWriter.Flush();
            memoryStream.Position = 0;
            using (var reader = new StreamReader(memoryStream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
    #endregion
}