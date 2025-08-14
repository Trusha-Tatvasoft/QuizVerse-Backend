using Xunit;
using Moq;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using Npgsql;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizVerse.UnitTests.Services;

public class QuestionPoolServiceTest
{
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock;
    private readonly Mock<IGenericRepository<BaseQuestion>> _baseQuestionRepoMock;
    private readonly Mock<IGenericRepository<QuestionOptionsAnswer>> _optionsRepoMock;
    private readonly Mock<IGenericRepository<QuestionType>> _typeRepoMock;
    private readonly Mock<IGenericRepository<QuestionDifficulty>> _difficultyRepoMock;
    private readonly Mock<IGenericRepository<QuizCategory>> _categoryRepoMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly QuestionPoolService _service;

    public QuestionPoolServiceTest()
    {
        _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
        _baseQuestionRepoMock = new Mock<IGenericRepository<BaseQuestion>>();
        _optionsRepoMock = new Mock<IGenericRepository<QuestionOptionsAnswer>>();
        _typeRepoMock = new Mock<IGenericRepository<QuestionType>>();
        _difficultyRepoMock = new Mock<IGenericRepository<QuestionDifficulty>>();
        _categoryRepoMock = new Mock<IGenericRepository<QuizCategory>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _mapperMock = new Mock<IMapper>();

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, "1")], "mock"))
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _service = new QuestionPoolService(
            _baseQuestionRepoMock.Object,
            _optionsRepoMock.Object,
            _typeRepoMock.Object,
            _difficultyRepoMock.Object,
            _categoryRepoMock.Object,
            _httpContextAccessorMock.Object,
            _mapperMock.Object,
            _sqlQueryRepoMock.Object
        );
    }

    private static QuestionRequestDTO GetValidCreateDto() => new()
    {
        QuestionTypeId = 2,
        CategoryId = 1,
        DifficultyId = 3,
        QuestionText = "Sample Question",
        Options = ["Opt1", "Opt2", "Opt3", "Opt4"],
        CorrectAnswer = "Opt1"
    };

    private static QuestionRequestDTO GetValidUpdateDto() => new()
    {
        QuestionTypeId = 2,
        CategoryId = 1,
        DifficultyId = 3,
        QuestionText = "Updated Sample Question",
        Options = ["Opt1", "Opt2", "Opt3", "Opt4"],
        CorrectAnswer = "Opt1"
    };

    private static List<QuestionImportDTO> CreateValidRecords()
    {
        return
        [
            new QuestionImportDTO
            {
                Question = "Q1",
                Category = "cat",
                Difficulty = "easy",
                Type = "multiple choice",
                CorrectAnswer = "Answer1",
                Option1 = "Option1",
                Option2 = "Option2"
            }
        ];
    }

    [Fact]
    public async Task CreateOrUpdateQuestion_Create_Success_ReturnsSuccessMessage()
    {
        QuestionRequestDTO dto = GetValidCreateDto();

        _categoryRepoMock
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>()))
            .ReturnsAsync(true);
        _typeRepoMock
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>()))
            .ReturnsAsync(true);
        _difficultyRepoMock
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()))
            .ReturnsAsync(true);

        _mapperMock
            .Setup(m => m.Map<BaseQuestion>(dto))
            .Returns(new BaseQuestion { Id = 10 });


        string result = await _service.CreateOrUpdateQuestion(0, dto);

        Assert.Equal(Constants.QUESTION_CREATION_SUCCESS_MESSAGE, result);

        _categoryRepoMock.Verify(r =>
            r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>()), Times.Once);
        _typeRepoMock.Verify(r =>
            r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>()), Times.Once);
        _difficultyRepoMock.Verify(r =>
            r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()), Times.Once);

        _baseQuestionRepoMock.Verify(r =>
            r.AddAsync(It.Is<BaseQuestion>(q =>
                q.CreatedBy == 1 &&
                q.CreatedDate != default &&
                q.Id == 10)), Times.Once);

        _optionsRepoMock.Verify(r =>
        r.AddRangeAsync(It.Is<List<QuestionOptionsAnswer>>(list =>
            list.Count == dto.Options!.Count + 1 &&
            dto.Options.All(opt => list.Any(o => o.Value == opt && o.QuestionId == 10)) &&
            list.Any(o => o.Value == dto.CorrectAnswer && o.Key.Equals("Answer", StringComparison.OrdinalIgnoreCase))
        )), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateQuestion_CategoryNotFound_ThrowsAppException()
    {
        QuestionRequestDTO dto = GetValidCreateDto();
        _categoryRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>())).ReturnsAsync(false);

        AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateQuestion(0, dto));

        Assert.Equal(string.Format(Constants.CATEGORY_NOT_FOUND, dto.CategoryId), ex.Message);
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateQuestion_QuestionTypeNotFound_ThrowsAppException()
    {
        QuestionRequestDTO dto = GetValidCreateDto();
        _categoryRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>())).ReturnsAsync(true);
        _typeRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>())).ReturnsAsync(false);

        AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateQuestion(0, dto));

        Assert.Equal(string.Format(Constants.QUESTION_TYPE_NOT_FOUND, dto.QuestionTypeId), ex.Message);
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateQuestion_DifficultyNotFound_ThrowsAppException()
    {
        QuestionRequestDTO dto = GetValidCreateDto();
        _categoryRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>())).ReturnsAsync(true);
        _typeRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>())).ReturnsAsync(true);
        _difficultyRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>())).ReturnsAsync(false);

        AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateQuestion(0, dto));

        Assert.Equal(string.Format(Constants.DIFFICULTY_NOT_FOUND, dto.DifficultyId), ex.Message);
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_NoOptionsProvided_AddsOnlyAnswer()
    {
        QuestionRequestDTO dto = GetValidCreateDto();
        dto.Options = null;
        dto.CorrectAnswer = "False";

        _categoryRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>())).ReturnsAsync(true);
        _typeRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>())).ReturnsAsync(true);
        _difficultyRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>())).ReturnsAsync(true);

        _mapperMock.Setup(m => m.Map<BaseQuestion>(dto)).Returns(new BaseQuestion { Id = 5 });

        string result = await _service.CreateOrUpdateQuestion(0, dto);

        Assert.Equal(Constants.QUESTION_CREATION_SUCCESS_MESSAGE, result);
        _optionsRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<QuestionOptionsAnswer>>(list =>
            list.Count == 1 &&
            list.Any(o => o.Value == dto.CorrectAnswer && o.Key.Equals(Constants.QUESTION_KEY_ANSWER, StringComparison.OrdinalIgnoreCase))
        )), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateQuestion_Update_Success_ReturnsSuccessMessage()
    {
        int questionId = 5;
        QuestionRequestDTO dto = GetValidUpdateDto();

        _categoryRepoMock
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>()))
            .ReturnsAsync(true);
        _typeRepoMock
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>()))
            .ReturnsAsync(true);
        _difficultyRepoMock
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()))
            .ReturnsAsync(true);

        BaseQuestion existingQuestion = new() { Id = questionId };
        _baseQuestionRepoMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null))
            .ReturnsAsync(existingQuestion);

        List<QuestionOptionsAnswer> existingOptions =
        [
            new() { Id = 1, QuestionId = questionId, Key = Constants.QUESTION_KEY_OPTION, Value = "Opt1" },
            new() { Id = 2, QuestionId = questionId, Key = Constants.QUESTION_KEY_OPTION, Value = "Opt2" },
            new() { Id = 3, QuestionId = questionId, Key = Constants.QUESTION_KEY_ANSWER, Value = "OldAnswer" }
        ];

        _optionsRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>()))
            .ReturnsAsync(existingOptions);

        string result = await _service.CreateOrUpdateQuestion(questionId, dto);

        Assert.Equal(Constants.QUESTION_UPDATE_SUCCESS_MESSAGE, result);

        _mapperMock.Verify(m => m.Map(dto, existingQuestion), Times.Once);

        _baseQuestionRepoMock.Verify(r =>
            r.UpdateAsync(It.Is<BaseQuestion>(q =>
                q.ModifiedBy == 1 &&
                q.ModifiedDate != default)), Times.Once);

        _optionsRepoMock.Verify(r =>
            r.AddRangeAsync(It.Is<List<QuestionOptionsAnswer>>(list =>
                list.Count == 2 &&
                list.Any(o => o.Value == "Opt3") &&
                list.Any(o => o.Value == "Opt4")
            )), Times.Once);

        _optionsRepoMock.Verify(r =>
            r.UpdateAsync(It.Is<QuestionOptionsAnswer>(o =>
                o.Key == Constants.QUESTION_KEY_ANSWER &&
                o.Value == dto.CorrectAnswer
            )), Times.Once);

        _optionsRepoMock.Verify(r =>
            r.UpdateRangeAsync(It.IsAny<List<QuestionOptionsAnswer>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateQuestion_QuestionNotFound_ThrowsAppException()
    {
        QuestionRequestDTO dto = GetValidUpdateDto();

        _categoryRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>())).ReturnsAsync(true);
        _typeRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>())).ReturnsAsync(true);
        _difficultyRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>())).ReturnsAsync(true);

        _baseQuestionRepoMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null))
            .ReturnsAsync((BaseQuestion)null!);

        AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateQuestion(1, dto));

        Assert.Equal(string.Format(Constants.QUESTION_NOT_FOUND_ERROR, 1), ex.Message);
    }

    [Fact]
    public async Task UpdateQuestion_NullOptions_AddsOnlyAnswer()
    {
        int questionId = 5;
        QuestionRequestDTO dto = GetValidUpdateDto();
        dto.Options = null;
        dto.CorrectAnswer = "False";

        _categoryRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizCategory, bool>>>())).ReturnsAsync(true);
        _typeRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionType, bool>>>())).ReturnsAsync(true);
        _difficultyRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>())).ReturnsAsync(true);

        _baseQuestionRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null)).ReturnsAsync(new BaseQuestion { Id = questionId });
        _optionsRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>())).ReturnsAsync([]);

        string result = await _service.CreateOrUpdateQuestion(questionId, dto);

        Assert.Equal(Constants.QUESTION_UPDATE_SUCCESS_MESSAGE, result);
        _optionsRepoMock.Verify(r =>
            r.AddRangeAsync(It.Is<List<QuestionOptionsAnswer>>(list =>
                list.Count == 1 &&
                list.Any(o => o.Value == dto.CorrectAnswer))), Times.Once);
    }

    [Fact]
    public async Task DeleteQuestion_WhenQuestionNotFound_ShouldThrowAppException()
    {
        _baseQuestionRepoMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null))
            .ReturnsAsync((BaseQuestion)null!);

        AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.DeleteQuestion(1));

        Assert.Equal(string.Format(Constants.QUESTION_NOT_FOUND_ERROR, 1), ex.Message);
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);

        _baseQuestionRepoMock.Verify(r => r.UpdateAsync(It.IsAny<BaseQuestion>()), Times.Never);
        _optionsRepoMock.Verify(r => r.UpdateAsync(It.IsAny<QuestionOptionsAnswer>()), Times.Never);
    }

    [Fact]
    public async Task DeleteQuestion_WhenNoOptionsExist_ShouldSoftDeleteQuestionOnly()
    {
        BaseQuestion question = new()
        {
            Id = 1,
            IsDeleted = false
        };

        _baseQuestionRepoMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null))
            .ReturnsAsync(question);

        _optionsRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>()))
            .ReturnsAsync([]);

        string result = await _service.DeleteQuestion(1);

        Assert.True(question.IsDeleted);
        Assert.Equal(1, question.ModifiedBy);
        Assert.NotNull(question.ModifiedDate);
        Assert.Equal(Constants.QUESTION_DELETE_SUCCESS_MESSAGE, result);

        _baseQuestionRepoMock.Verify(r => r.UpdateAsync(question), Times.Once);
        _optionsRepoMock.Verify(r => r.UpdateAsync(It.IsAny<QuestionOptionsAnswer>()), Times.Never);
    }

    [Fact]
    public async Task DeleteQuestion_WhenOptionsExist_ShouldSoftDeleteQuestionAndOptions()
    {
        BaseQuestion question = new()
        {
            Id = 1,
            IsDeleted = false
        };

        List<QuestionOptionsAnswer> options =
        [
            new() {Id = 1, IsDeleted = false},
            new() {Id = 2, IsDeleted = false}
        ];

        _baseQuestionRepoMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null))
            .ReturnsAsync(question);

        _optionsRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>()))
            .ReturnsAsync(options);

        string result = await _service.DeleteQuestion(1);

        Assert.True(question.IsDeleted);
        Assert.All(options, o => Assert.True(o.IsDeleted));
        Assert.All(options, o => Assert.Equal(1, o.ModifiedBy));
        Assert.All(options, o => Assert.NotNull(o.ModifiedDate));
        Assert.Equal(Constants.QUESTION_DELETE_SUCCESS_MESSAGE, result);

        _baseQuestionRepoMock.Verify(r => r.UpdateAsync(question), Times.Once);
        _optionsRepoMock.Verify(r => r.UpdateAsync(It.IsAny<QuestionOptionsAnswer>()), Times.Exactly(options.Count));
    }

    [Fact]
    public async Task DeleteQuestion_WhenSomeOptionsAlreadyDeleted_ShouldOnlyUpdateActiveOnes()
    {
        BaseQuestion question = new()
        {
            Id = 1,
            IsDeleted = false
        };

        QuestionOptionsAnswer activeOption = new()
        {
            QuestionId = 1,
            IsDeleted = false
        };

        QuestionOptionsAnswer deletedOption = new()
        {
            QuestionId = 2,
            IsDeleted = true
        };

        _baseQuestionRepoMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), null))
            .ReturnsAsync(question);

        _optionsRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>()))
            .ReturnsAsync(new List<QuestionOptionsAnswer> { activeOption });

        var result = await _service.DeleteQuestion(1);

        // Assert
        Assert.True(activeOption.IsDeleted);
        Assert.Equal(1, activeOption.ModifiedBy);
        Assert.Equal(Constants.QUESTION_DELETE_SUCCESS_MESSAGE, result);

        _optionsRepoMock.Verify(r => r.UpdateAsync(It.IsAny<QuestionOptionsAnswer>()), Times.Once);
    }

    [Fact]
    public async Task GetQuestionPreview_QuestionNotFound_ThrowsAppException()
    {
        int questionId = 123;

        _baseQuestionRepoMock.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<BaseQuestion, bool>>>(),
                It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
            .ReturnsAsync((BaseQuestion?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(async () =>
        {
            await _service.GetQuestionPreview(questionId);
        });

        Assert.Equal(string.Format(Constants.QUESTION_NOT_FOUND_ERROR, questionId), ex.Message);

        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetQuestionPreview_MultipleChoiceQuestion_ReturnsDtoWithOptions()
    {
        int questionId = 1;
        BaseQuestion question = new()
        {
            Id = questionId,
            QueType = new QuestionType
            {
                TypeName = Constants.QUESTION_TYPE_MULTIPLE_CHOICE
            },
            Category = new QuizCategory
            {
                CategoryName = "Cat"
            },
            QueDifficulty = new QuestionDifficulty
            {
                Name = "Easy"
            }
        };

        List<QuestionOptionsAnswer> optionsAndAnswers =
        [
            new() {
                Key = Constants.QUESTION_KEY_OPTION,
                Value = "Option1"
            },
            new() {
                Key = Constants.QUESTION_KEY_OPTION,
                Value = "Option2"
            },
            new() {
                Key = Constants.QUESTION_KEY_ANSWER,
                Value = "Option2"
            }
        ];

        QuestionDetailDTO mappedDto = new()
        {
            QuestionType = Constants.QUESTION_TYPE_MULTIPLE_CHOICE,
            Options = null
        };

        _baseQuestionRepoMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<BaseQuestion, bool>>>(),
            It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
            .ReturnsAsync(question);

        _optionsRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>()))
            .ReturnsAsync(optionsAndAnswers);

        _mapperMock.Setup(m => m.Map<QuestionDetailDTO>(question)).Returns(mappedDto);

        QuestionDetailDTO? result = await _service.GetQuestionPreview(questionId);

        Assert.NotNull(result);
        Assert.Equal("Option2", result.CorrectAnswer);

        Assert.NotNull(result.Options);
        Assert.Equal(2, result.Options.Count);

        Assert.Equal("A.", result.Options[0].Label);
        Assert.Equal("Option1", result.Options[0].Value);
        Assert.False(result.Options[0].IsCorrect);

        Assert.Equal("B.", result.Options[1].Label);
        Assert.Equal("Option2", result.Options[1].Value);
        Assert.True(result.Options[1].IsCorrect);
    }

    [Fact]
    public async Task GetQuestionPreview_NonMultipleChoiceQuestion_ReturnsDtoWithNullOptions()
    {
        int questionId = 1;
        BaseQuestion questionEntity = new()
        {
            Id = questionId,
            QueType = new QuestionType
            {
                TypeName = "True/False"
            },
            Category = new QuizCategory
            {
                CategoryName = "Cat"
            },
            QueDifficulty = new QuestionDifficulty
            {
                Name = "Medium"
            }
        };

        List<QuestionOptionsAnswer> optionsAndAnswers = new List<QuestionOptionsAnswer>
        {
            new() {
                Key = Constants.QUESTION_KEY_OPTION,
                Value = "True"
            },
            new() {
                Key = Constants.QUESTION_KEY_ANSWER,
                Value = "True"
            }
        };

        QuestionDetailDTO mappedDto = new()
        {
            QuestionType = "TrueFalse",
            Options = [new() {
                Label = "A.",
                Value = "True",
                IsCorrect = true }]
        };

        _baseQuestionRepoMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<BaseQuestion, bool>>>(),
            It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
            .ReturnsAsync(questionEntity);

        _optionsRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuestionOptionsAnswer, bool>>>()))
            .ReturnsAsync(optionsAndAnswers);

        _mapperMock.Setup(m => m.Map<QuestionDetailDTO>(questionEntity)).Returns(mappedDto);

        QuestionDetailDTO? result = await _service.GetQuestionPreview(questionId);

        Assert.NotNull(result);
        Assert.Equal("True", result.CorrectAnswer);
        Assert.Null(result.Options);
    }

    [Fact]
    public async Task ImportQuestionsFromCsv_EmptyFile_ThrowsAppException()
    {
        string csvHeaderOnly = "Question,Type,Difficulty,Category,Option1,Option2,Option3,Option4,CorrectAnswer\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvHeaderOnly));

        AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.ImportQuestionsFromCsv(stream));

        Assert.Equal(Constants.CSV_INVALID_OR_EMPTY_ERROR, ex.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task ImportQuestionsFromExcel_EmptyFile_ThrowsAppException()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "Question";
        worksheet.Cell(1, 2).Value = "Category";
        worksheet.Cell(1, 3).Value = "Difficulty";
        worksheet.Cell(1, 4).Value = "Type";
        worksheet.Cell(1, 5).Value = "Option1";
        worksheet.Cell(1, 6).Value = "Option2";
        worksheet.Cell(1, 7).Value = "Option3";
        worksheet.Cell(1, 8).Value = "Option4";
        worksheet.Cell(1, 9).Value = "CorrectAnswer";

        using MemoryStream ms = new();
        workbook.SaveAs(ms);
        ms.Position = 0;

        AppException ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ImportQuestionsFromExcel(ms));

        Assert.Equal(Constants.EXCEL_INVALID_OR_EMPTY_ERROR, ex.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task ImportQuestionsFromCsv_ValidFile_ReturnsSuccessMessage()
    {
        string csvContent =
            "Question,Category,Difficulty,Type,Option1,Option2,Option3,Option4,CorrectAnswer\n" +
            "What is 2+2?,Math,Easy,MCQ,1,2,3,4,4\n";

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuizCategory>
        {
            new() { Id = 1, CategoryName = "math" }
        });

        _difficultyRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuestionDifficulty>
        {
            new() { Id = 1, Name = "easy" }
        });

        _typeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuestionType>
        {
            new() { Id = 1, TypeName = "mcq" }
        });

        _baseQuestionRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<BaseQuestion>>()))
            .Returns(Task.CompletedTask);

        _optionsRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<QuestionOptionsAnswer>>()))
            .Returns(Task.CompletedTask);

        string result = await _service.ImportQuestionsFromCsv(stream);

        Assert.Contains("imported successfully", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Constants.CSV, result);
    }

    [Fact]
    public async Task ImportQuestionsFromExcel_ValidFile_ReturnsSuccessMessage()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "Question";
        worksheet.Cell(1, 2).Value = "Category";
        worksheet.Cell(1, 3).Value = "Difficulty";
        worksheet.Cell(1, 4).Value = "Type";
        worksheet.Cell(1, 5).Value = "Option1";
        worksheet.Cell(1, 6).Value = "Option2";
        worksheet.Cell(1, 7).Value = "Option3";
        worksheet.Cell(1, 8).Value = "Option4";
        worksheet.Cell(1, 9).Value = "CorrectAnswer";

        worksheet.Cell(2, 1).Value = "What is 2+2?";
        worksheet.Cell(2, 2).Value = "Math";
        worksheet.Cell(2, 3).Value = "Easy";
        worksheet.Cell(2, 4).Value = "MCQ";
        worksheet.Cell(2, 5).Value = "1";
        worksheet.Cell(2, 6).Value = "2";
        worksheet.Cell(2, 7).Value = "3";
        worksheet.Cell(2, 8).Value = "4";
        worksheet.Cell(2, 9).Value = "4";

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        stream.Position = 0;

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuizCategory>
        {
            new() { Id = 1, CategoryName = "math" }
        });

        _difficultyRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuestionDifficulty>
        {
            new() { Id = 1, Name = "easy" }
        });

        _typeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuestionType>
        {
            new() { Id = 1, TypeName = "mcq" }
        });

        _baseQuestionRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<BaseQuestion>>()))
            .Returns(Task.CompletedTask);

        _optionsRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<QuestionOptionsAnswer>>()))
            .Returns(Task.CompletedTask);

        string result = await _service.ImportQuestionsFromExcel(stream);

        Assert.Contains("imported successfully", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Constants.EXCEL, result);
    }

    [Fact]
    public async Task GetQuestionPoolListAsync_ReturnsPaginatedList()
    {
        // Arrange
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "sample",
            SortColumn = "queText",
            SortDescending = false,
            Filters = new FilterDto
            {
                QuizCategoryId = 1,
                QuestionDifficultyId = 2,
                QuestionTypeId = 3
            }
        };

        var questionPoolList = new List<QuestionPoolListDto>
            {
                new() { Id = 1, QueText = "Sample Question 1" },
                new() { Id = 2, QueText = "Sample Question 2" }
            };

        var totalRecords = new TotalRecordsDto { TotalRecords = 3 };

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(questionPoolList);

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(totalRecords);

        // Act
        var result = await _service.GetQuestionPoolListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(2, result.Records.Count);
    }

    [Fact]
    public async Task GetQuestionPoolListAsync_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = null,
            SortColumn = null,
            SortDescending = false,
            Filters = null
        };

        var totalRecords = new TotalRecordsDto { TotalRecords = 0 };

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new List<QuestionPoolListDto>());

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(totalRecords);

        // Act
        var result = await _service.GetQuestionPoolListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRecords);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task GetQuestionPoolListAsync_NullFilters_DoesNotThrow()
    {
        // Arrange
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 5,
            Filters = null
        };

        var totalRecords = new TotalRecordsDto { TotalRecords = 0 };

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new List<QuestionPoolListDto>());

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(totalRecords);

        // Act
        var result = await _service.GetQuestionPoolListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task PreviewQuestionsFromCsv_ValidCsv_ReturnsPreviewList()
    {
        string csvContent = "Question,Category,Difficulty,Type,CorrectAnswer,Option1,Option2,Option3,Option4\n" + "What is 2+2?,Math,Easy,MCQ,4,1,2,3,4";

        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        _categoryRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(
            [
                new() { Id = 1, CategoryName  = "Math" }
            ]);

        _difficultyRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(
            [
                new() { Id = 1, Name = "Easy" }
            ]);

        _typeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(
            [
                new() { Id = 1, TypeName  = "MCQ" }
            ]);

        List<QuestionsListResponseDto> result = await _service.PreviewQuestionsFromCsv(stream);

        Assert.NotNull(result);
        Assert.Single(result);

        QuestionsListResponseDto question = result[0];
        Assert.Equal("What is 2+2?", question.QueText);
        Assert.Equal(1, question.CategoryId);
        Assert.Equal(1, question.QueDifficultyId);
        Assert.Equal(1, question.QueTypeId);
        Assert.Contains(question.QueOptionsAns, o => o.Value == "4" && o.Key == Constants.QUESTION_KEY_ANSWER);
        Assert.Equal(4, question.QueOptionsAns.Count(o => o.Key == Constants.QUESTION_KEY_OPTION));
    }

    [Fact]
    public async Task PreviewQuestionsFromCsv_EmptyCsv_ThrowsAppException()
    {
        string csvContent = "Question,Category,Difficulty,Type,CorrectAnswer\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));

        AppException exception = await Assert.ThrowsAsync<AppException>(() =>
            _service.PreviewQuestionsFromCsv(stream));

        Assert.Equal(Constants.CSV_INVALID_OR_EMPTY_ERROR, exception.Message);
    }

    [Fact]
    public async Task PreviewQuestionsFromExcel_ValidExcel_ReturnsPreviewList()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "Question";
        worksheet.Cell(1, 2).Value = "Category";
        worksheet.Cell(1, 3).Value = "Difficulty";
        worksheet.Cell(1, 4).Value = "Type";
        worksheet.Cell(1, 5).Value = "Option1";
        worksheet.Cell(1, 6).Value = "Option2";
        worksheet.Cell(1, 7).Value = "Option3";
        worksheet.Cell(1, 8).Value = "Option4";
        worksheet.Cell(1, 9).Value = "CorrectAnswer";

        worksheet.Cell(2, 1).Value = "What is 2+2?";
        worksheet.Cell(2, 2).Value = "Math";
        worksheet.Cell(2, 3).Value = "Easy";
        worksheet.Cell(2, 4).Value = "MCQ";
        worksheet.Cell(2, 5).Value = "1";
        worksheet.Cell(2, 6).Value = "2";
        worksheet.Cell(2, 7).Value = "3";
        worksheet.Cell(2, 8).Value = "4";
        worksheet.Cell(2, 9).Value = "4";

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        stream.Position = 0;

        _categoryRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync([new() { Id = 1, CategoryName = "Math" }]);
        _difficultyRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync([new() { Id = 1, Name = "Easy" }]);
        _typeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync([new() { Id = 1, TypeName = "MCQ" }]);

        List<QuestionsListResponseDto> result = await _service.PreviewQuestionsFromExcel(stream);

        Assert.NotNull(result);
        Assert.Single(result);

        QuestionsListResponseDto question = result[0];
        Assert.Equal("What is 2+2?", question.QueText);
        Assert.Equal(1, question.CategoryId);
        Assert.Equal(1, question.QueDifficultyId);
        Assert.Equal(1, question.QueTypeId);

        Assert.Contains(question.QueOptionsAns, o => o.Value == "4" && o.Key == Constants.QUESTION_KEY_ANSWER);
        Assert.Equal(4, question.QueOptionsAns.Count(o => o.Key == Constants.QUESTION_KEY_OPTION));
    }

    [Fact]
    public async Task PreviewQuestionsFromExcel_EmptyExcel_ThrowsAppException()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "Question";
        worksheet.Cell(1, 2).Value = "Category";
        worksheet.Cell(1, 3).Value = "Difficulty";
        worksheet.Cell(1, 4).Value = "Type";
        worksheet.Cell(1, 5).Value = "Option1";
        worksheet.Cell(1, 6).Value = "Option2";
        worksheet.Cell(1, 7).Value = "Option3";
        worksheet.Cell(1, 8).Value = "Option4";
        worksheet.Cell(1, 9).Value = "CorrectAnswer";

        using MemoryStream ms = new();
        workbook.SaveAs(ms);
        ms.Position = 0;

        AppException exception = await Assert.ThrowsAsync<AppException>(() =>
            _service.PreviewQuestionsFromExcel(ms));

        Assert.Equal(Constants.EXCEL_INVALID_OR_EMPTY_ERROR, exception.Message);
    }
}

