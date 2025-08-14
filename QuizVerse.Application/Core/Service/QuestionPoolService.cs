using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Globalization;
using AutoMapper;
using CsvHelper;
using ExcelDataReader;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Common;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Service;

public class QuestionPoolService(
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
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.USER_NOT_AUTHENTICATED_MESSAGE);

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

        List<QuestionPoolListDto> questionPools = await _sqlQueryRepository.SqlQueryListAsync<QuestionPoolListDto>(query, parameters);
        int totalRecords = _baseQuestionRepository.GetQueryableInclude().Count();

        PageListResponse<QuestionPoolListDto> response = new()
        {
            TotalRecords = totalRecords,
            Records = questionPools
        };

        return response;
    }

    public async Task<string> ImportQuestionsFromCsv(Stream fileStream)
    {
        List<QuestionImportDTO> records = ReadCsv(fileStream);
        if (records.Count == 0)
            throw new AppException(Constants.CSV_INVALID_OR_EMPTY_ERROR, StatusCodes.Status400BadRequest);

        return await ImportRecordsAsync(records, Constants.CSV);
    }

    public async Task<string> ImportQuestionsFromExcel(Stream fileStream)
    {
        List<QuestionImportDTO> records = ReadExcel(fileStream);
        if (records.Count == 0)
            throw new AppException(Constants.EXCEL_INVALID_OR_EMPTY_ERROR, StatusCodes.Status400BadRequest);

        return await ImportRecordsAsync(records, Constants.EXCEL);
    }

    public async Task<List<QuestionsListResponseDto>> PreviewQuestionsFromCsv(Stream fileStream)
    {
        List<QuestionImportDTO> records = ReadCsv(fileStream);
        if (records.Count == 0)
            throw new AppException(Constants.CSV_INVALID_OR_EMPTY_ERROR, StatusCodes.Status400BadRequest);

        (Dictionary<string, int> categoryMap, Dictionary<string, int> difficultyMap, Dictionary<string, int> typeMap) = await LoadMappingsAsync();

        return GetValidQuestionsPreview(records, categoryMap, difficultyMap, typeMap);
    }

    public async Task<List<QuestionsListResponseDto>> PreviewQuestionsFromExcel(Stream fileStream)
    {
        List<QuestionImportDTO> records = ReadExcel(fileStream);
        if (records.Count == 0)
            throw new AppException(Constants.EXCEL_INVALID_OR_EMPTY_ERROR, StatusCodes.Status400BadRequest);

        (Dictionary<string, int> categoryMap, Dictionary<string, int> difficultyMap, Dictionary<string, int> typeMap) = await LoadMappingsAsync();

        return GetValidQuestionsPreview(records, categoryMap, difficultyMap, typeMap);
    }
    #endregion

    #region Import Core
    private async Task<string> ImportRecordsAsync(
        List<QuestionImportDTO> records, string sourceName)
    {
        (Dictionary<string, int> categoryMap, Dictionary<string, int> difficultyMap, Dictionary<string, int> typeMap) = await LoadMappingsAsync();

        (List<BaseQuestion> questions, List<QuestionOptionsAnswer> options) = ProcessImportRecords(records, categoryMap, difficultyMap, typeMap);

        if (questions.Count == 0)
            throw new AppException(string.Format(Constants.NO_VALID_QUESTIONS_FOUND_IN_FILE_ERROR, sourceName), StatusCodes.Status400BadRequest);

        foreach (BaseQuestion question in questions)
        {
            question.CreatedBy = UserId;
            question.CreatedDate = DateTime.UtcNow;
        }

        foreach (QuestionOptionsAnswer option in options)
        {
            option.CreatedBy = UserId;
            option.CreatedDate = DateTime.UtcNow;
        }

        await _baseQuestionRepository.AddRangeAsync(questions);
        await _questionOptionsAnswerRepository.AddRangeAsync(options);

        return string.Format(Constants.QUESTIONS_IMPORTED_SUCCESS_MESSAGE, questions.Count, sourceName);
    }

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

    private static (List<BaseQuestion> Questions, List<QuestionOptionsAnswer> Options) ProcessImportRecords(
        List<QuestionImportDTO> records,
        Dictionary<string, int> categoryMap,
        Dictionary<string, int> difficultyMap,
        Dictionary<string, int> typeMap)
    {
        List<BaseQuestion> questions = [];
        List<QuestionOptionsAnswer> options = [];

        foreach (QuestionImportDTO record in records)
        {
            if (!IsValidRecord(record, categoryMap, difficultyMap, typeMap, out var categoryId, out var difficultyId, out var typeId))
                continue;

            BaseQuestion question = new()
            {
                QueText = record.Question!,
                CategoryId = categoryId,
                QueDifficultyId = difficultyId,
                QueTypeId = typeId,
            };
            questions.Add(question);

            IEnumerable<string> validOptions = record.GetOptions().Where(o => !string.IsNullOrWhiteSpace(o));
            options.AddRange(validOptions.Select(o => new QuestionOptionsAnswer
            {
                Question = question,
                Key = Constants.QUESTION_KEY_OPTION,
                Value = o!,
            }));

            options.Add(new QuestionOptionsAnswer
            {
                Question = question,
                Key = Constants.QUESTION_KEY_ANSWER,
                Value = record.CorrectAnswer!,
            });
        }

        return (questions, options);
    }
    #endregion

    #region File Reading
    private static List<QuestionImportDTO> ReadCsv(Stream fileStream)
    {
        using StreamReader reader = new(fileStream);
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        string[] required = ["Question", "Category", "Difficulty", "Type", "CorrectAnswer"];

        if (csv.HeaderRecord == null || !required.All(col => csv.HeaderRecord.Contains(col)))
            return [];

        return [.. csv.GetRecords<QuestionImportDTO>()];
    }

    private static List<QuestionImportDTO> ReadExcel(Stream fileStream)
    {
        using IExcelDataReader reader = ExcelReaderFactory.CreateReader(fileStream);
        DataSet dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        DataTable table = dataSet.Tables[0];

        string[] required = ["Question", "Category", "Difficulty", "Type", "CorrectAnswer"];

        if (!required.All(table.Columns.Contains))
            return [];

        return [.. table.Rows.Cast<DataRow>()
            .Select(row => new QuestionImportDTO
            {
                Question = row["Question"]?.ToString(),
                Category = row["Category"]?.ToString(),
                Difficulty = row["Difficulty"]?.ToString(),
                Type = row["Type"]?.ToString(),
                CorrectAnswer = row["CorrectAnswer"]?.ToString(),
                Option1 = table.Columns.Contains("Option1") ? row["Option1"]?.ToString() : null,
                Option2 = table.Columns.Contains("Option2") ? row["Option2"]?.ToString() : null,
                Option3 = table.Columns.Contains("Option3") ? row["Option3"]?.ToString() : null,
                Option4 = table.Columns.Contains("Option4") ? row["Option4"]?.ToString() : null
            })];
    }
    #endregion

    #region Validation & Creation
    private QuestionOptionsAnswer CreateOption(string value, int questionId) =>
        new()
        {
            QuestionId = questionId,
            Key = Constants.QUESTION_KEY_OPTION,
            Value = value,
            CreatedBy = UserId,
            CreatedDate = DateTime.UtcNow
        };

    private QuestionOptionsAnswer CreateAnswer(string value, int questionId) =>
        new()
        {
            QuestionId = questionId,
            Key = Constants.QUESTION_KEY_ANSWER,
            Value = value,
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
                    QueDifficultyId = difficultyId,
                    QueTypeId = typeId,
                    QueOptionsAns = optionsDto
                });
            }
        }

        return previewList;
    }
}
