using Npgsql;
using NpgsqlTypes;
using System.Data;
using AutoMapper;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Interface;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using ClosedXML.Excel;

namespace QuizVerse.Application.Core.Service;

public class QuestionPoolService(
    IGenericRepository<QuizToBaseQuestionMap> _quizToBaseQuestionMapRepository,
    IGenericRepository<QuizPlayStatus> _quizPlayStatusRepository,
    IGenericRepository<BattleList> _battleListRepository,
    IGenericRepository<BattleStatus> _battleStatusRepository,
    IGenericRepository<BaseQuestion> _baseQuestionRepository,
    IGenericRepository<QuestionOptionsAnswer> _questionOptionsAnswerRepository,
    IGenericRepository<QuestionType> _questionTypeRepository,
    IGenericRepository<QuestionDifficulty> _questionDifficultyRepository,
    IGenericRepository<QuizCategory> _quizCategoryRepository,
    IHttpContextAccessor httpContextAccessor,
    IMapper _mapper,
    ISqlQueryRepository _sqlQueryRepository
) : IQuestionPoolService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region Question Management
    public async Task<string> CreateOrUpdateQuestion(int id, QuestionRequestDTO dto)
    {
        if (!await _quizCategoryRepository.Exists(c => c.Id == dto.CategoryId))
            throw new AppException(string.Format(Constants.CATEGORY_NOT_FOUND, dto.CategoryId), StatusCodes.Status404NotFound);

        if (!await _questionTypeRepository.Exists(qt => qt.Id == dto.QuestionTypeId))
            throw new AppException(string.Format(Constants.QUESTION_TYPE_NOT_FOUND, dto.QuestionTypeId), StatusCodes.Status404NotFound);

        if (!await _questionDifficultyRepository.Exists(d => d.Id == dto.DifficultyId))
            throw new AppException(string.Format(Constants.DIFFICULTY_NOT_FOUND, dto.DifficultyId), StatusCodes.Status404NotFound);

        ValidateOptionsAndAnswer(dto.Options, dto.CorrectAnswer);

        BaseQuestion? question;
        bool isCreate = id == 0;

        if (isCreate)
        {
            question = _mapper.Map<BaseQuestion>(dto);
            question.CreatedBy = UserId;
            question.CreatedDate = DateTime.UtcNow;

            await _baseQuestionRepository.AddAsync(question);
            id = question.Id;

            List<QuestionOptionsAnswer> options = dto.Options?.Select(opt => CreateOption(opt, id)).ToList() ?? [];
            options.Add(CreateAnswer(dto.CorrectAnswer, id));
            await _questionOptionsAnswerRepository.AddRangeAsync(options);
        }
        else
        {
            bool isInUse = await _quizToBaseQuestionMapRepository
                .GetQueryableInclude()
                .Where(map => map.QueId == id && !map.IsDeleted)
                .AnyAsync(map =>
                    _quizPlayStatusRepository.GetQueryableInclude().Any(qps => qps.QuizId == map.QuizId && qps.IsCompleted == false) ||
                    _battleListRepository.GetQueryableInclude().Any(bl => bl.QuizId == map.QuizId && !bl.IsDeleted &&
                        _battleStatusRepository.GetQueryableInclude().Any(bs => bs.BattleId == bl.Id && !bs.IsDeleted &&
                            bs.BattleStatus1 == (int)Infrastructure.Enums.BattleStatus.Running))
            );

            if (isInUse)
                throw new AppException(Constants.QUESTION_IN_USE_ERROR, StatusCodes.Status400BadRequest);

            question = await _baseQuestionRepository.GetAsync(q => q.Id == id && !q.IsDeleted);

            if (question == null)
                throw new AppException(string.Format(Constants.QUESTION_NOT_FOUND_ERROR, id), StatusCodes.Status404NotFound);

            _mapper.Map(dto, question);
            question.ModifiedBy = UserId;
            question.ModifiedDate = DateTime.UtcNow;

            await _baseQuestionRepository.UpdateAsync(question);

            List<QuestionOptionsAnswer> existingOptions = await _questionOptionsAnswerRepository.FindAsync(o => o.QuestionId == id && !o.IsDeleted);

            List<string> dtoOptions = dto.Options?.Select(o => o.Trim()).ToList() ?? [];

            List<QuestionOptionsAnswer> optionsToDelete = [.. existingOptions
                .Where(e => e.Key == Constants.QUESTION_KEY_OPTION && !dtoOptions
                    .Any(o => string.Equals(o, e.Value, StringComparison.OrdinalIgnoreCase)))];

            foreach (QuestionOptionsAnswer opt in optionsToDelete)
            {
                opt.IsDeleted = true;
                opt.ModifiedBy = UserId;
                opt.ModifiedDate = DateTime.UtcNow;
            }

            List<QuestionOptionsAnswer> optionsToAdd = [.. dtoOptions
                .Where(o => !existingOptions
                    .Any(e => e.Key == Constants.QUESTION_KEY_OPTION && string.Equals(e.Value, o, StringComparison.OrdinalIgnoreCase)))
                .Select(o => CreateOption(o, id))];

            QuestionOptionsAnswer? existingAnswer = existingOptions.FirstOrDefault(e => e.Key == Constants.QUESTION_KEY_ANSWER);

            if (existingAnswer != null &&
                !string.Equals(existingAnswer.Value, dto.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                existingAnswer.Value = dto.CorrectAnswer.Trim();
                existingAnswer.ModifiedBy = UserId;
                existingAnswer.ModifiedDate = DateTime.UtcNow;
            }
            else if (existingAnswer == null)
            {
                optionsToAdd.Add(CreateAnswer(dto.CorrectAnswer, id));
            }

            if (optionsToDelete.Count != 0)
                await _questionOptionsAnswerRepository.UpdateRangeAsync(optionsToDelete);

            if (optionsToAdd.Count != 0)
                await _questionOptionsAnswerRepository.AddRangeAsync(optionsToAdd);

            if (existingAnswer != null)
                await _questionOptionsAnswerRepository.UpdateAsync(existingAnswer);
        }

        return isCreate
            ? Constants.QUESTION_CREATION_SUCCESS_MESSAGE
            : Constants.QUESTION_UPDATE_SUCCESS_MESSAGE;
    }

    public async Task<string> DeleteQuestion(int id)
    {
        BaseQuestion? question = await _baseQuestionRepository
            .GetAsync(q => q.Id == id && !q.IsDeleted);

        if (question == null)
            throw new AppException(string.Format(Constants.QUESTION_NOT_FOUND_ERROR, id), StatusCodes.Status404NotFound);

        // Check if question is part of any ongoing quiz or live battle
        bool isInUse = await _quizToBaseQuestionMapRepository
            .GetQueryableInclude()
            .Where(map => map.QueId == id && !map.IsDeleted)
            .AnyAsync(map =>
                _quizPlayStatusRepository.GetQueryableInclude().Any(qps => qps.QuizId == map.QuizId && qps.IsCompleted == false) ||
                _battleListRepository.GetQueryableInclude().Any(bl => bl.QuizId == map.QuizId && !bl.IsDeleted &&
                    _battleStatusRepository.GetQueryableInclude().Any(bs => bs.BattleId == bl.Id && !bs.IsDeleted &&
                        bs.BattleStatus1 == (int)Infrastructure.Enums.BattleStatus.Running))
            );

        if (isInUse)
            throw new AppException(Constants.QUESTION_IN_USE_ERROR, StatusCodes.Status400BadRequest);


        question.IsDeleted = true;
        question.ModifiedBy = UserId;
        question.ModifiedDate = DateTime.UtcNow;
        await _baseQuestionRepository.UpdateAsync(question);

        List<QuestionOptionsAnswer> options = await _questionOptionsAnswerRepository
            .FindAsync(o => o.QuestionId == id && !o.IsDeleted);

        foreach (QuestionOptionsAnswer opt in options)
        {
            opt.IsDeleted = true;
            opt.ModifiedBy = UserId;
            opt.ModifiedDate = DateTime.UtcNow;
            await _questionOptionsAnswerRepository.UpdateAsync(opt);
        }

        return Constants.QUESTION_DELETE_SUCCESS_MESSAGE;
    }

    public async Task<QuestionDetailDTO?> GetQuestionPreview(int id)
    {
        BaseQuestion? question = await _baseQuestionRepository.GetAsync(
            q => q.Id == id && !q.IsDeleted,
            includes: q => q
                .Include(q => q.Category)
                .Include(q => q.QueDifficulty)
                .Include(q => q.QueType)
        );

        if (question == null)
            throw new AppException(string.Format(Constants.QUESTION_NOT_FOUND_ERROR, id), StatusCodes.Status404NotFound);

        QuestionDetailDTO previewDto = _mapper.Map<QuestionDetailDTO>(question);

        List<QuestionOptionsAnswer> optionsAndAnswers = await _questionOptionsAnswerRepository.FindAsync(
            o => o.QuestionId == id && !o.IsDeleted
        );

        string correctAnswerValue = optionsAndAnswers.FirstOrDefault(o => o.Key == Constants.QUESTION_KEY_ANSWER)?.Value!;

        if (string.Equals(previewDto.QuestionType, Constants.QUESTION_TYPE_MULTIPLE_CHOICE, StringComparison.OrdinalIgnoreCase))
        {
            previewDto.Options = [.. optionsAndAnswers
                .Where(o => o.Key == Constants.QUESTION_KEY_OPTION)
                .Select((opt, index) => new QuestionOptionDTO
                {
                    Label = $"{(char)('A' + index)}.",
                    Value = opt.Value,
                    IsCorrect = string.Equals(opt.Value, correctAnswerValue, StringComparison.OrdinalIgnoreCase)
                })];
        }
        else
        {
            previewDto.Options = null;
        }

        previewDto.CorrectAnswer = correctAnswerValue ?? string.Empty;

        return previewDto;
    }

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

    public async Task<string> SaveQuestions(List<QuestionsListRequestDto> questionList)
    {
        if (questionList == null || questionList.Count == 0)
            return Constants.NO_QUESTIONS_TO_SAVE;

        foreach (QuestionsListRequestDto question in questionList)
        {
            BaseQuestion baseQuestion = _mapper.Map<BaseQuestion>(question);
            baseQuestion.CreatedBy = UserId;
            baseQuestion.CreatedDate = DateTime.UtcNow;
            baseQuestion.IsDeleted = false;

            await _baseQuestionRepository.AddAsync(baseQuestion);

            if (question.QueOptionsAns != null && question.QueOptionsAns.Count > 0)
            {
                List<QuestionOptionsAnswer> options = _mapper.Map<List<QuestionOptionsAnswer>>(question.QueOptionsAns);

                options.ForEach(opt =>
                {
                    opt.QuestionId = baseQuestion.Id;
                    opt.CreatedBy = UserId;
                    opt.CreatedDate = DateTime.UtcNow;
                    opt.IsDeleted = false;
                });

                await _questionOptionsAnswerRepository.AddRangeAsync(options);
            }
        }

        return Constants.QUESTIONS_SAVED_SUCCESSFULLY;
    }

    public async Task<List<QuestionsListResponseDto>> PreviewQuestionsFromCsv(Stream fileStream)
    {
        List<QuestionImportDTO> records = ReadQuestions(fileStream, ".csv");
        if (records.Count == 0)
            throw new AppException(Constants.CSV_INVALID_OR_EMPTY_ERROR, StatusCodes.Status400BadRequest);

        (Dictionary<string, int> categoryMap, Dictionary<string, int> difficultyMap, Dictionary<string, int> typeMap) = await LoadMappingsAsync();

        return GetValidQuestionsPreview(records, categoryMap, difficultyMap, typeMap);
    }

    public async Task<List<QuestionsListResponseDto>> PreviewQuestionsFromExcel(Stream fileStream)
    {
        List<QuestionImportDTO> records = ReadQuestions(fileStream, ".xlsx");
        if (records.Count == 0)
            throw new AppException(Constants.EXCEL_INVALID_OR_EMPTY_ERROR, StatusCodes.Status400BadRequest);

        (Dictionary<string, int> categoryMap, Dictionary<string, int> difficultyMap, Dictionary<string, int> typeMap) = await LoadMappingsAsync();

        return GetValidQuestionsPreview(records, categoryMap, difficultyMap, typeMap);
    }
    #endregion

    #region Import Mappings
    private async Task<(Dictionary<string, int> CategoryMap, Dictionary<string, int> DifficultyMap, Dictionary<string, int> TypeMap)>
        LoadMappingsAsync()
    {
        List<QuizCategory> categories = await _quizCategoryRepository.GetAllAsync();
        List<QuestionDifficulty> difficulties = await _questionDifficultyRepository.GetAllAsync();
        List<QuestionType> types = await _questionTypeRepository.GetAllAsync();

        return (
            categories.ToDictionary(c => c.CategoryName.Trim().ToLowerInvariant(), c => c.Id),
            difficulties.ToDictionary(d => d.Name.Trim().ToLowerInvariant(), d => d.Id),
            types.ToDictionary(t => t.TypeName.Trim().ToLowerInvariant(), t => t.Id)
        );
    }
    #endregion

    #region File Reading
    private static List<QuestionImportDTO> ReadQuestions(Stream fileStream, string fileType)
    {
        List<QuestionImportDTO> result = [];
        IXLWorksheet worksheet;

        if (fileType.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            using StreamReader reader = new(fileStream);
            using XLWorkbook workbook = new();
            worksheet = workbook.AddWorksheet("CSV");

            int rowIndex = 1;
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] values = line.Split(',');

                for (int colIndex = 0; colIndex < values.Length; colIndex++)
                    worksheet.Cell(rowIndex, colIndex + 1).Value = values[colIndex].Trim();

                rowIndex++;
            }

            result = ParseWorksheet(worksheet);
        }
        else
        {
            using XLWorkbook workbook = new(fileStream);
            worksheet = workbook.Worksheets.First();

            result = ParseWorksheet(worksheet);
        }

        return result;
    }

    private static List<QuestionImportDTO> ParseWorksheet(IXLWorksheet worksheet)
    {
        List<QuestionImportDTO> result = [];

        IXLRow headerRow = worksheet.Row(1);
        Dictionary<string, int> headers = headerRow.Cells()
            .Where(c => !string.IsNullOrWhiteSpace(c.GetString()))
            .Select((c, index) => new { Name = c.GetString().Trim(), Index = index + 1 })
            .ToDictionary(x => x.Name, x => x.Index);

        string[] required = ["Question", "Category", "Difficulty", "Type", "CorrectAnswer"];

        if (!required.All(headers.ContainsKey))
            return [];

        foreach (IXLRow row in worksheet.RowsUsed().Skip(1))
        {
            QuestionImportDTO dto = new()
            {
                Question = row.Cell(headers["Question"]).GetString(),
                Category = row.Cell(headers["Category"]).GetString(),
                Difficulty = row.Cell(headers["Difficulty"]).GetString(),
                Type = row.Cell(headers["Type"]).GetString(),
                CorrectAnswer = row.Cell(headers["CorrectAnswer"]).GetString(),
                Option1 = headers.TryGetValue("Option1", out int op1) ? row.Cell(op1).GetString() : null,
                Option2 = headers.TryGetValue("Option2", out int op2) ? row.Cell(op2).GetString() : null,
                Option3 = headers.TryGetValue("Option3", out int op3) ? row.Cell(op3).GetString() : null,
                Option4 = headers.TryGetValue("Option4", out int op4) ? row.Cell(op4).GetString() : null
            };

            result.Add(dto);
        }

        return result;
    }
    #endregion

    #region Validation & Creation
    private QuestionOptionsAnswer CreateOption(string value, int questionId) =>
        new()
        {
            QuestionId = questionId,
            Key = Constants.QUESTION_KEY_OPTION,
            Value = value.Trim(),
            CreatedBy = UserId,
            CreatedDate = DateTime.UtcNow
        };

    private QuestionOptionsAnswer CreateAnswer(string value, int questionId) =>
        new()
        {
            QuestionId = questionId,
            Key = Constants.QUESTION_KEY_ANSWER,
            Value = value.Trim(),
            CreatedBy = UserId,
            CreatedDate = DateTime.UtcNow
        };

    private static void ValidateOptionsAndAnswer(List<string>? options, string correctAnswer)
    {
        if (options == null || options.Count == 0) return;

        List<string> emptyOptions = [.. options.Where(o => string.IsNullOrWhiteSpace(o))];

        if (emptyOptions.Count != 0)
        {
            throw new AppException(
                Constants.OPTIONS_CANNOT_BE_EMPTY,
                StatusCodes.Status400BadRequest
            );
        }

        List<string> normalizedOptions = [.. options.Select(o => o?.Trim() ?? string.Empty)];

        List<string> duplicates = [.. normalizedOptions
            .GroupBy(o => o, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)];

        if (duplicates.Count != 0)
        {
            throw new AppException(
                string.Format(Constants.DUPLICATE_OPTIONS_FOUND, string.Join(", ", duplicates)),
                StatusCodes.Status400BadRequest
            );
        }

        if (!normalizedOptions.Any(o =>
            string.Equals(o, correctAnswer?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
        {
            throw new AppException(
                string.Format(Constants.CORRECT_ANSWER_NOT_IN_OPTIONS, correctAnswer),
                StatusCodes.Status400BadRequest
            );
        }
    }

    private static bool IsValidRecord(
        QuestionImportDTO record,
        Dictionary<string, int> categoryMap,
        Dictionary<string, int> difficultyMap,
        Dictionary<string, int> typeMap,
        out int categoryId,
        out int difficultyId,
        out int typeId)
    {
        categoryId = difficultyId = typeId = 0;

        if (string.IsNullOrWhiteSpace(record.Question) ||
            string.IsNullOrWhiteSpace(record.Category) ||
            string.IsNullOrWhiteSpace(record.Difficulty) ||
            string.IsNullOrWhiteSpace(record.Type) ||
            string.IsNullOrWhiteSpace(record.CorrectAnswer))
            return false;

        string categoryKey = record.Category.Trim().ToLowerInvariant();
        string difficultyKey = record.Difficulty.Trim().ToLowerInvariant();
        string typeKey = record.Type.Trim().ToLowerInvariant();

        if (!categoryMap.TryGetValue(categoryKey, out categoryId) ||
            !difficultyMap.TryGetValue(difficultyKey, out difficultyId) ||
            !typeMap.TryGetValue(typeKey, out typeId))
            return false;

        try
        {
            ValidateOptionsAndAnswer(record.GetOptions(), record.CorrectAnswer);
        }
        catch (AppException)
        {
            return false;
        }

        return true;
    }
    #endregion

    private static List<QuestionsListResponseDto> GetValidQuestionsPreview(
    List<QuestionImportDTO> records,
    Dictionary<string, int> categoryMap,
    Dictionary<string, int> difficultyMap,
    Dictionary<string, int> typeMap)
    {
        List<QuestionsListResponseDto> previewList = [];

        foreach (QuestionImportDTO record in records)
        {
            if (IsValidRecord(record, categoryMap, difficultyMap, typeMap,
                out int categoryId, out int difficultyId, out int typeId))
            {
                List<QueOptionsAndAnswersDto> optionsDto = [.. record.GetOptions()
                    .Where(option => !string.IsNullOrWhiteSpace(option))
                    .Select(option => new QueOptionsAndAnswersDto
                    {
                        Key = Constants.QUESTION_KEY_OPTION,
                        Value = option.Trim()
                    })];

                if (!string.IsNullOrWhiteSpace(record.CorrectAnswer))
                {
                    optionsDto.Add(new QueOptionsAndAnswersDto
                    {
                        Key = Constants.QUESTION_KEY_ANSWER,
                        Value = record.CorrectAnswer.Trim()
                    });
                }

                previewList.Add(new QuestionsListResponseDto
                {
                    QueText = record.Question?.Trim() ?? string.Empty,
                    CategoryId = categoryId,
                    CategoryName = record.Category!,
                    QueDifficultyId = difficultyId,
                    QueDifficultyName = record.Difficulty!,
                    QueTypeId = typeId,
                    QueTypeName = record.Type!,
                    QueOptionsAns = optionsDto
                });
            }
        }

        return previewList;
    }
}
