using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Mappings;
using QuizVerse.Infrastructure.Repository;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class QuizManagementServiceTests
{
    private readonly QuizVerseDbContext _context;
    private readonly QuizManagementService _quizService;
    private readonly IMapper _mapper;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ISqlQueryRepository> _sqlRepoMock;

    public QuizManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new QuizVerseDbContext(options);
        SeedTestData();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(x => x.HttpContext)
            .Returns(new DefaultHttpContext()); // can add claims if needed
        _sqlRepoMock = new Mock<ISqlQueryRepository>();

        var quizRepo = new GenericRepository<Quiz>(_context);
        var questionTypeRepo = new GenericRepository<QuestionType>(_context);
        var questionDifficultyRepo = new GenericRepository<QuestionDifficulty>(_context);
        var quizCategoryRepo = new GenericRepository<QuizCategory>(_context);

        _quizService = new QuizManagementService(
            quizRepo,
            questionTypeRepo,
            questionDifficultyRepo,
            quizCategoryRepo,
            _mapper,
            _httpContextAccessorMock.Object,
            _sqlRepoMock.Object
        );
    }

    private void SeedTestData()
    {
        var category = new QuizCategory
        {
            Id = 1,
            CategoryName = "General Knowledge",
            Description = "Science related quizzes",
            Status = true,
            IsDeleted = false,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        var difficulty = new QuizDifficulty
        {
            Id = 1,
            Name = "Easy",
            Description = "Easy level questions",
            IsDeleted = false,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        if (!_context.QuizCategories.Any())
            _context.QuizCategories.Add(category);

        if (!_context.QuizDifficulties.Any())
            _context.QuizDifficulties.Add(difficulty);

        _context.SaveChanges();

        var quiz1 = new Quiz
        {
            Id = 1,
            Name = "Quiz 1",
            Description = "This is quiz 1",
            Status = (int)QuizStatus.Active,
            CategoryId = category.Id,
            DifficultyLevelId = difficulty.Id,
            IsDeleted = false
        };

        var quiz2 = new Quiz
        {
            Id = 2,
            Name = "Quiz 2",
            Description = "This is quiz 2",
            Status = (int)QuizStatus.Inactive,
            CategoryId = category.Id,
            DifficultyLevelId = difficulty.Id,
            IsDeleted = false
        };

        _context.Quizzes.AddRange(quiz1, quiz2);
        _context.SaveChanges();

        // Add attempt data after quizzes exist
        var quizAttempteds = new List<QuizAttempted>
    {
        new QuizAttempted { QuizId = quiz1.Id, UserId = 1, CreatedDate = DateTime.Now },
        new QuizAttempted { QuizId = quiz1.Id, UserId = 2, CreatedDate = DateTime.Now },
        new QuizAttempted { QuizId = quiz2.Id, UserId = 1, CreatedDate = DateTime.Now }
    };

        var questionMaps = new List<QuizToBaseQuestionMap>
    {
        new QuizToBaseQuestionMap { QuizId = quiz1.Id, QueId = 1 },
        new QuizToBaseQuestionMap { QuizId = quiz1.Id, QueId = 2 },
        new QuizToBaseQuestionMap { QuizId = quiz2.Id, QueId = 2 },
        new QuizToBaseQuestionMap { QuizId = quiz2.Id, QueId = 3 }
    };

        _context.QuizAttempteds.AddRange(quizAttempteds);
        _context.QuizToBaseQuestionMaps.AddRange(questionMaps);
        _context.SaveChanges();
    }

    // #region Quiz Card Data
    // [Fact]
    // public async Task GetQuizCardData_ReturnsCorrectStats()
    // {
    //     var result = await _quizService.GetQuizCardData();

    //     Assert.Equal(2, result.TotalQuiz);
    //     Assert.Equal(1, result.ActiveQuiz);
    //     Assert.Equal(2, result.TotalParticipants);
    //     Assert.Equal(3, result.TotalQuestions);
    // }
    // #endregion
    // #region Quiz Management List
    // [Fact]
    // public async Task GetQuizzesByPagination_WithSearchTerm_ReturnsFilteredResults()
    // {
    //     var request = new PageListRequest
    //     {
    //         PageNumber = 1,
    //         PageSize = 10,
    //         SearchTerm = "Quiz 1"
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Single(result.Records);
    //     Assert.Equal("Quiz 1", result.Records.First().QuizTitle);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithInvalidStatus_ThrowsAppException()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizStatus = (QuizStatus)999 }
    //     };

    //     var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.GetQuizzesByPagination(request));
    //     Assert.Equal(Constants.INVALID_QUIZ_STATUS_MESSAGE, ex.Message);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithCategoryFilter_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizCategoryId = 1 }
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithDifficultyFilter_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizDifficultyId = 1 }
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_SortingDescending_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         SortColumn = "name",
    //         SortDescending = true
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    //     Assert.Equal("Quiz 2", result.Records.First().QuizTitle);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithValidStatusFilter_ReturnsCorrectResults()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizStatus = QuizStatus.Active }
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Single(result.Records);
    //     Assert.Equal((int)QuizStatus.Active, result.Records.First().Status);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_SortByCategory_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         SortColumn = "category",
    //         SortDescending = false
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    //     Assert.All(result.Records, r => Assert.Equal("General Knowledge", r.CategoryName));
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_SortByDifficulty_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         SortColumn = "difficulty",
    //         SortDescending = false
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    //     Assert.All(result.Records, r => Assert.Equal("Easy", r.QuizDifficultyLevel));
    // }
    // #endregion

    #region MoveQuizzesToCategoryAsync
    [Fact]
    public async Task MoveQuizzesToCategoryAsync_UpdatesCategoryIds()
    {
        var categoryWithQuizzes = new QuizCategory
        {
            Id = 1,
            Quizzes = _context.Quizzes.ToList()
        };

        await _quizService.MoveQuizzesToCategoryAsync(categoryWithQuizzes, 99);

        var all = _context.Quizzes.ToList();
        Assert.All(all, q => Assert.Equal(99, q.CategoryId));
    }

    [Fact]
    public async Task MoveQuizzesToCategoryAsync_NullInput_NoException()
    {
        await _quizService.MoveQuizzesToCategoryAsync(null!, 99);
    }
    #endregion

    #region CreateUpdateQuiz
    [Fact]
    public async Task CreateUpdateQuiz_ValidRequest_ReturnsResponse()
    {
        var request = new SaveQuizRequestDto
        {
            Id = 1,
            Name = "Updated Quiz",
            CategoryId = 1,
            Description = "Desc",
            TotalTime = 10,
            DifficultyLevelId = 1,
            TotalQuestion = 5,
            IsPaid = false,
            Status = (int)QuizStatus.Active
        };

        var expectedResponse = new CreateUpdateResponseDto { Success = true, Message = "Success" };
        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(expectedResponse);

        var result = await _quizService.CreateUpdateQuiz(request);
        Assert.Equal("Success", result.Message);
    }

    [Fact]
    public async Task CreateUpdateQuiz_NullRequest_Throws()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _quizService.CreateUpdateQuiz(null!));
        Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
    }
    #endregion

    #region GetQuizDataById
    [Fact]
    public async Task GetQuizDataById_ValidId_ReturnsDto()
    {
        var expectedDto = new QuizResponseDto { Id = 1, Name = "Quiz 1" };
        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<QuizResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(expectedDto);

        var result = await _quizService.GetQuizDataById(1);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetQuizDataById_InvalidId_Throws()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _quizService.GetQuizDataById(0));
        Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
    }
    #endregion

    #region DeleteQuiz
    [Fact]
    public async Task DeleteQuiz_ValidId_CallsSqlRepoAndReturnsResponse()
    {
        var expectedResponse = new CreateUpdateResponseDto { Success = true, Message = "Deleted" };
        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(expectedResponse);

        var result = await _quizService.DeleteQuiz(1);

        Assert.True(result.Success);
        Assert.Equal("Deleted", result.Message);
        _sqlRepoMock.Verify(s =>
            s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.Is<string>(q => q.Contains(SqlConstants.DELETE_QUIZ_FUNCTION)),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteQuiz_InvalidId_ThrowsAppException()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.DeleteQuiz(0));
        Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
    }
    #endregion

    #region ExportQuestionsToCsv Tests
    [Fact]
    public async Task ExportQuestionsToCsv_NullQuestions_ThrowsAppException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(null));
        Assert.Equal("No questions provided", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_EmptyQuestions_ThrowsAppException()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(new List<QuestionsListRequestDto>()));
        Assert.Equal("No questions provided", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MissingQuestionText_ThrowsAppException()
    {
        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "   ",
            QueTypeId = 1,
            QueDifficultyId = 1,
            CategoryId = 1
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.Equal("Question text is missing", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_InvalidTypeId_ThrowsAppException()
    {
        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 999,
            QueDifficultyId = 1,
            CategoryId = 1
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.StartsWith("Invalid Question Type ID", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_InvalidDifficultyId_ThrowsAppException()
    {
        // Add valid QuestionType
        _context.QuestionTypes.Add(new QuestionType { Id = 1, TypeName = "Multiple Choice" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 1,
            QueDifficultyId = 999,
            CategoryId = 1
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.StartsWith("Invalid Difficulty ID", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_InvalidCategoryId_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 1, TypeName = "Multiple Choice" });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 1, Name = "Easy", Description = "Easy difficulty" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 1,
            QueDifficultyId = 1,
            CategoryId = 999
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.StartsWith("Invalid Category ID", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MultipleChoiceTooFewOptions_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 2, TypeName = "Multiple Choice" });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 2, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 2, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 2,
            QueDifficultyId = 2,
            CategoryId = 2,
            QueOptionsAns = new List<QueOptionsAndAnswersDto>
            {
                new() { Key = "option", Value = "One" }
            }
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.Equal("Multiple choice question must have at least 2 options", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MultipleChoiceMissingAnswer_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 3, TypeName = "Multiple Choice" });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 3, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 3, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 3,
            QueDifficultyId = 3,
            CategoryId = 3,
            QueOptionsAns = new List<QueOptionsAndAnswersDto>
            {
                new() { Key = "option", Value = "One" },
                new() { Key = "option", Value = "Two" }
            }
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.Equal("Correct answer is missing for multiple choice question", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_TrueFalseMissingAnswer_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 4, TypeName = "True/False" });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 4, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 4, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 4,
            QueDifficultyId = 4,
            CategoryId = 4
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.Equal("Correct answer is missing for true/false question", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_ShortAnswerMissingAnswer_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 5, TypeName = "Short Answer" });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 5, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 5, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Q1",
            QueTypeId = 5,
            QueDifficultyId = 5,
            CategoryId = 5
        }
    };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(questions));
        Assert.Equal("Correct answer is missing for short answer question", ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MultipleChoicePadsOptions_ReturnsCsv()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 6, TypeName = "Multiple Choice" });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 6, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 6, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var questions = new List<QuestionsListRequestDto>
    {
        new QuestionsListRequestDto
        {
            QueText = "Capital of France?",
            QueTypeId = 6,
            QueDifficultyId = 6,
            CategoryId = 6,
            QueOptionsAns = new List<QueOptionsAndAnswersDto>
            {
                new() { Key = "option", Value = "Paris" },
                new() { Key = "option", Value = "Lyon" },
                new() { Key = "answer", Value = "Paris" }
            }
        }
    };

        var csv = await _quizService.ExportQuestionsToCsv(questions);
        Assert.Contains("Capital of France?", csv);
        Assert.Contains("Paris", csv);
        Assert.Contains("Lyon", csv);
    }
    #endregion
}
