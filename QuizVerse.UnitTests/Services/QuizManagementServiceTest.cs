using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Interface;
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
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ISqlQueryRepository> _sqlRepoMock;
    private readonly Mock<IDropDownDataService> _dropDownDataServiceMock;
    private readonly Mock<ICommonService> _commonServiceMock;

    public QuizManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new QuizVerseDbContext(options);
        SeedTestData();

        _mockMapper = new Mock<IMapper>();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.UserData, "1") }, "mock"));
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _sqlRepoMock = new Mock<ISqlQueryRepository>();
        _dropDownDataServiceMock = new Mock<IDropDownDataService>();
        _commonServiceMock = new Mock<ICommonService>();

        var quizRepo = new GenericRepository<Quiz>(_context);
        var questionTypeRepo = new GenericRepository<QuestionType>(_context);
        var questionDifficultyRepo = new GenericRepository<QuestionDifficulty>(_context);
        var quizCategoryRepo = new GenericRepository<QuizCategory>(_context);

        _quizService = new QuizManagementService(
            quizRepo,
            questionTypeRepo,
            questionDifficultyRepo,
            quizCategoryRepo,
            _mockMapper.Object,
            _httpContextAccessorMock.Object,
            _sqlRepoMock.Object,
            _dropDownDataServiceMock.Object,
            _commonServiceMock.Object
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

    #region Get Card Data
    [Fact]
    public async Task GetQuizCardData_ReturnsExpectedCounts_FromSqlRepository()
    {
        // Arrange - mock the SQL repository result
        var expectedDto = new QuizManagementPageDataDto
        {
            TotalQuiz = 2,
            ActiveQuiz = 1,
            TotalParticipants = 2,
            TotalQuestions = 3
        };

        _sqlRepoMock
            .Setup(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _quizService.GetQuizCardData();

        // Assert
        Assert.Equal(expectedDto.TotalQuiz, result.TotalQuiz);
        Assert.Equal(expectedDto.ActiveQuiz, result.ActiveQuiz);
        Assert.Equal(expectedDto.TotalParticipants, result.TotalParticipants);
        Assert.Equal(expectedDto.TotalQuestions, result.TotalQuestions);

        // Verify that the repository was called with the expected parameter
        _sqlRepoMock.Verify(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
               It.IsAny<string>(),
               It.Is<NpgsqlParameter[]>(p =>
                   p.Length == 2 &&
                   p.Any(x => x.ParameterName == "p_active_status" &&
                              Convert.ToInt32(x.Value) == (int)QuizStatus.Active) &&
                   p.Any(x => x.ParameterName == "p_quiz_type" &&
                              Convert.ToInt32(x.Value) == (int)QuizType.Normal)
               )
           ), Times.Once);
    }

    [Fact]
    public async Task GetQuizCardData_ReturnsEmptyCounts_WhenNoRecordsExist()
    {
        // Arrange - repo returns "empty row" from SQL
        var emptyDto = new QuizManagementPageDataDto
        {
            TotalQuiz = 0,
            ActiveQuiz = 0,
            TotalParticipants = 0,
            TotalQuestions = 0
        };

        _sqlRepoMock
            .Setup(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(emptyDto);

        // Act
        var result = await _quizService.GetQuizCardData();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalQuiz);
        Assert.Equal(0, result.ActiveQuiz);
        Assert.Equal(0, result.TotalParticipants);
        Assert.Equal(0, result.TotalQuestions);

        // Verify repository called with both parameters
        _sqlRepoMock.Verify(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
            It.IsAny<string>(),
            It.Is<NpgsqlParameter[]>(p =>
                p.Length == 2 &&
                p.Any(x => x.ParameterName == "p_active_status") &&
                p.Any(x => x.ParameterName == "p_quiz_type")
            )
        ), Times.Once);
    }
    #endregion

    #region Get Quiz List
    [Fact]
    public async Task GetQuizzesByPagination_ReturnsMappedResults()
    {
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "math",
            SortColumn = "TotalQuestion",
            SortDescending = true,
            Filters = new FilterDto
            {
                QuizStatus = QuizStatus.Active,
                QuizCategoryId = 2,
                QuizDifficultyId = 3
            }
        };

        var dbQuizzes = new List<QuizListDto>
            {
                new() { Id = 1, QuizTitle = "Math Quiz 1" },
                new() { Id = 2, QuizTitle = "Math Quiz 2" }
            };

        _sqlRepoMock
            .Setup(r => r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(dbQuizzes);

        _sqlRepoMock
            .Setup(r => r.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new TotalRecordsDto { TotalRecords = 100 });

        _mockMapper
            .Setup(m => m.Map<List<QuizListDto>>(dbQuizzes))
            .Returns(dbQuizzes);

        // Act
        var result = await _quizService.GetQuizzesByPagination(request);

        // Assert
        Assert.Equal(100, result.TotalRecords);
        Assert.Equal(2, result.Records.Count);
        Assert.Equal("Math Quiz 1", result.Records[0].QuizTitle);
    }

    [Fact]
    public async Task GetQuizzesByPagination_EmptyResults_ReturnsZeroTotal()
    {
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 5
        };

        _sqlRepoMock
            .Setup(r => r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new List<QuizListDto>());

        _sqlRepoMock
            .Setup(r => r.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

        _mockMapper
            .Setup(m => m.Map<List<QuizListDto>>(It.IsAny<List<QuizListDto>>()))
            .Returns(new List<QuizListDto>());

        // Act
        var result = await _quizService.GetQuizzesByPagination(request);

        // Assert
        Assert.Empty(result.Records);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task GetQuizzesByPagination_Parameters_CorrectlyMapped()
    {
        var request = new PageListRequest
        {
            PageNumber = 3,
            PageSize = 20,
            SearchTerm = "history",
            SortColumn = "QuizTitle",
            SortDescending = true,
            Filters = new FilterDto
            {
                QuizStatus = QuizStatus.Active,
                QuizCategoryId = 2,
                QuizDifficultyId = 5
            }
        };

        _sqlRepoMock.Setup(r =>
            r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(new List<QuizListDto>());

        _sqlRepoMock.Setup(r =>
            r.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

        // Act
        await _quizService.GetQuizzesByPagination(request);

        // Assert all parameters
        _sqlRepoMock.Verify(r =>
            r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p =>
                    Convert.ToInt32(p.First(x => x.ParameterName == "p_page_number").Value) == 3 &&
                    Convert.ToInt32(p.First(x => x.ParameterName == "p_page_size").Value) == 20 &&
                    Convert.ToString(p.First(x => x.ParameterName == "p_search_term").Value) == "history" &&
                    Convert.ToString(p.First(x => x.ParameterName == "p_sort_column").Value) == "QuizTitle" &&
                    Convert.ToBoolean(p.First(x => x.ParameterName == "p_sort_descending").Value) == true &&
                    Convert.ToInt32(p.First(x => x.ParameterName == "p_quiz_status").Value) == (int)QuizStatus.Active &&
                    Convert.ToInt32(p.First(x => x.ParameterName == "p_category_id").Value) == 2 &&
                    Convert.ToInt32(p.First(x => x.ParameterName == "p_difficulty_id").Value) == 5
                )
            ), Times.Once);

    }

    [Fact]
    public async Task GetQuizzesByPagination_OptionalParametersNull_UsesDBNull()
    {
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = null,
            SortColumn = null,
            SortDescending = false,
            Filters = null
        };

        _sqlRepoMock.Setup(r =>
            r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(new List<QuizListDto>());

        _sqlRepoMock.Setup(r =>
            r.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

        // Act
        await _quizService.GetQuizzesByPagination(request);

        // Assert DBNull.Value for null parameters
        _sqlRepoMock.Verify(r =>
            r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p =>
                    p.First(x => x.ParameterName == "p_search_term").Value == DBNull.Value &&
                    p.First(x => x.ParameterName == "p_sort_column").Value == DBNull.Value &&
                    p.First(x => x.ParameterName == "p_quiz_status").Value == DBNull.Value &&
                    p.First(x => x.ParameterName == "p_category_id").Value == DBNull.Value &&
                    p.First(x => x.ParameterName == "p_difficulty_id").Value == DBNull.Value
                )
            ), Times.Once);
    }

    [Fact]
    public async Task GetQuizzesByPagination_EmptyFilters_DefaultToDBNull()
    {
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new FilterDto() // all properties null / default
        };

        _sqlRepoMock.Setup(r =>
            r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(new List<QuizListDto>());

        _sqlRepoMock.Setup(r =>
            r.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()
            ))
            .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

        // Act
        await _quizService.GetQuizzesByPagination(request);

        _sqlRepoMock.Verify(r =>
            r.SqlQueryListAsync<QuizListDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p =>
                    p.First(x => x.ParameterName == "p_quiz_status").Value == DBNull.Value &&
                    p.First(x => x.ParameterName == "p_category_id").Value == DBNull.Value &&
                    p.First(x => x.ParameterName == "p_difficulty_id").Value == DBNull.Value
                )
            ), Times.Once);
    }
    #endregion

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

    #region CreateUpdateQuiz Tests
    [Fact]
    public async Task CreateUpdateQuiz_ValidRequest_ReturnsResponse_AndClearsCache()
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

        Assert.True(result.Success);
        Assert.Equal("Success", result.Message);

        // Verify cache is cleared when successful
        _dropDownDataServiceMock.Verify(s => s.ClearCache(DropDownType.QuizTag), Times.Once);
    }

    [Fact]
    public async Task CreateUpdateQuiz_NullRequest_ThrowsAppException()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.CreateUpdateQuiz(null!));
        Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task CreateUpdateQuiz_SqlReturnsNull_ThrowsAppException()
    {
        var request = new SaveQuizRequestDto
        {
            Name = "New Quiz",
            CategoryId = 1,
            Description = "Desc",
            TotalTime = 10,
            DifficultyLevelId = 1,
            TotalQuestion = 5,
            IsPaid = false,
            Status = (int)QuizStatus.Active
        };

        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((CreateUpdateResponseDto)null!);

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.CreateUpdateQuiz(request));

        Assert.Equal(Constants.CREATE_OR_UPDATE_QUIZ_FAILED, ex.Message);
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task CreateUpdateQuiz_SqlReturnsFailure_ThrowsAppException()
    {
        var request = new SaveQuizRequestDto
        {
            Name = "New Quiz",
            CategoryId = 1,
            Description = "Desc",
            TotalTime = 10,
            DifficultyLevelId = 1,
            TotalQuestion = 5,
            IsPaid = false,
            Status = (int)QuizStatus.Active
        };

        var failureResponse = new CreateUpdateResponseDto { Success = false, Message = "Validation failed" };
        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(failureResponse);

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.CreateUpdateQuiz(request));

        Assert.Equal("Validation failed", ex.Message);
        Assert.Equal(400, ex.StatusCode);
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

    #region UpdateQuizAction Tests

    [Fact]
    public async Task UpdateQuizAction_ValidDeleteAction_CallsSqlRepoAndReturnsResponse()
    {
        // Arrange
        var expectedResponse = new CreateUpdateResponseDto { Success = true, Message = "Deleted successfully" };
        var actionDto = new QuizActionDataDto
        {
            Id = 1,
            Action = UserActionType.Delete,
            NewStatus = null
        };

        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _quizService.UpdateQuizAction(actionDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Deleted successfully", result.Message);

        _sqlRepoMock.Verify(s =>
            s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.Is<string>(q => q.Contains(SqlConstants.UPDATE_QUIZ_ACTION_QUERY_FUNCTION)),
                It.Is<NpgsqlParameter[]>(p =>
                    p.Any(x => x.ParameterName == "p_quiz_id" && (int)x.Value == 1) &&
                    p.Any(x => x.ParameterName == "p_is_deleted_action" && (bool)x.Value == true)
                )
            ),
            Times.Once);
    }

    [Fact]
    public async Task UpdateQuizAction_SqlReturnsNull_ThrowsAppException()
    {
        // Arrange
        var actionDto = new QuizActionDataDto
        {
            Id = 1,
            Action = UserActionType.Delete
        };

        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((CreateUpdateResponseDto)null!);

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.UpdateQuizAction(actionDto));

        // Assert
        Assert.Equal(Constants.DELETE_QUIZ_FAILED, ex.Message);
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizAction_SqlReturnsFailureResponse_ThrowsAppException()
    {
        // Arrange
        var failureResponse = new CreateUpdateResponseDto { Success = false, Message = "Quiz cannot be deleted" };
        var actionDto = new QuizActionDataDto
        {
            Id = 1,
            Action = UserActionType.Delete
        };

        _sqlRepoMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(failureResponse);

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.UpdateQuizAction(actionDto));

        // Assert
        Assert.Equal("Quiz cannot be deleted", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    #endregion

    #region ExportQuestionsToCsv Tests
    [Fact]
    public async Task ExportQuestionsToCsv_EmptyQuizName_ThrowsAppException()
    {
        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "", // empty name
            Questions = new List<QuestionsListRequestDto> { new QuestionsListRequestDto { QueText = "Q1" } }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(Constants.INVALID_EXPORT_REQUEST_QUIZNAME, ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_WhitespaceQuizName_ThrowsAppException()
    {
        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "   ", // only whitespace
            Questions = new List<QuestionsListRequestDto> { new QuestionsListRequestDto { QueText = "Q1" } }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(Constants.INVALID_EXPORT_REQUEST_QUIZNAME, ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_NullQuestions_ThrowsAppException()
    {
        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = null!
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(Constants.INVALID_EXPORT_REQUEST_QUESTIONS, ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_EmptyQuestions_ThrowsAppException()
    {
        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>()
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(Constants.INVALID_EXPORT_REQUEST_QUESTIONS, ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MissingQuestionText_ThrowsAppException()
    {
        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "   ",
                    QueTypeId = 1,
                    QueDifficultyId = 1,
                    CategoryId = 1
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(Constants.MISSING_QUESTION_TEXT, ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_InvalidTypeId_ThrowsAppException()
    {
        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "Q1",
                    QueTypeId = 999,
                    QueDifficultyId = 1,
                    CategoryId = 1
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.StartsWith(string.Format(Constants.INVALID_QUESTION_TYPE_ID, request.Questions[0].QueTypeId), ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_InvalidDifficultyId_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 1, TypeName = Constants.QUESTION_TYPE_MULTIPLE_CHOICE });
        _context.SaveChanges();

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "Q1",
                    QueTypeId = 1,
                    QueDifficultyId = 999,
                    CategoryId = 1
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.StartsWith(string.Format(Constants.INVALID_QUESTION_DIFFICULTY_ID, request.Questions[0].QueDifficultyId), ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_InvalidCategoryId_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 1, TypeName = Constants.QUESTION_TYPE_MULTIPLE_CHOICE });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 1, Name = "Easy", Description = "Easy difficulty" });
        _context.SaveChanges();

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "Q1",
                    QueTypeId = 1,
                    QueDifficultyId = 1,
                    CategoryId = 999
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.StartsWith(string.Format(Constants.INVALID_CATEGORY_ID, request.Questions[0].CategoryId), ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MultipleChoiceTooFewOptions_ThrowsAppException()
    {
        _context.QuestionTypes.Add(new QuestionType { Id = 2, TypeName = Constants.QUESTION_TYPE_MULTIPLE_CHOICE });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 2, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 2, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "Q1",
                    QueTypeId = 2,
                    QueDifficultyId = 2,
                    CategoryId = 2,
                    QueOptionsAns = new List<QueOptionsAndAnswersDto>
                    {
                        new() { Key = "option1", Value = "One" }
                    }
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(Constants.INVALID_MCQ_OPTIONS, ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MultipleChoiceMissingAnswer_ThrowsAppException()
    {
        var type = new QuestionType { Id = 3, TypeName = Constants.QUESTION_TYPE_MULTIPLE_CHOICE };
        _context.QuestionTypes.Add(type);
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 3, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 3, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
        {
            new QuestionsListRequestDto
            {
                QueText = "Q1",
                QueTypeId = 3,
                QueDifficultyId = 3,
                CategoryId = 3,
                QueOptionsAns = new List<QueOptionsAndAnswersDto>
                {
                    new() { Key = "option1", Value = "One" },
                    new() { Key = "option2", Value = "Two" }
                }
            }
        }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));

        Assert.Equal(string.Format(Constants.NO_CORRECT_ANSWER, type.TypeName.ToLower()), ex.Message);
    }


    [Fact]
    public async Task ExportQuestionsToCsv_TrueFalseMissingAnswer_ThrowsAppException()
    {
        var type = new QuestionType { Id = 4, TypeName = Constants.QUESTION_TYPE_TRUE_FALSE };
        _context.QuestionTypes.Add(type);
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 4, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 4, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "Q1",
                    QueTypeId = 4,
                    QueDifficultyId = 4,
                    CategoryId = 4
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(string.Format(Constants.NO_CORRECT_ANSWER, type.TypeName.ToLower()), ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_ShortAnswerMissingAnswer_ThrowsAppException()
    {
        var type = new QuestionType { Id = 5, TypeName = Constants.QUESTION_TYPE_SHORT_ANSWER };
        _context.QuestionTypes.Add(type);
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 5, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 5, CategoryName = "Science", Description = "General science trivia" });
        _context.SaveChanges();

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
            {
                new QuestionsListRequestDto
                {
                    QueText = "Q1",
                    QueTypeId = 5,
                    QueDifficultyId = 5,
                    CategoryId = 5
                }
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.ExportQuestionsToCsv(request));
        Assert.Equal(string.Format(Constants.NO_CORRECT_ANSWER, type.TypeName.ToLower()), ex.Message);
    }

    [Fact]
    public async Task ExportQuestionsToCsv_MultipleChoicePadsOptions_ReturnsCsv()
    {
        // Arrange
        // Configure ICommonService.EscapeCsv mock
        _commonServiceMock.Setup(x => x.EscapeCsv(It.IsAny<string>()))
            .Returns<string>(input =>
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                bool mustQuote = input.Contains(",") || input.Contains("\"") || input.Contains("\n");
                if (mustQuote)
                {
                    input = input.Replace("\"", "\"\"");
                    return $"\"{input}\"";
                }
                return input;
            });

        _context.QuestionTypes.Add(new QuestionType { Id = 6, TypeName = Constants.QUESTION_TYPE_MULTIPLE_CHOICE });
        _context.QuestionDifficulties.Add(new QuestionDifficulty { Id = 6, Name = "Easy", Description = "Easy difficulty" });
        _context.QuizCategories.Add(new QuizCategory { Id = 6, CategoryName = "Science", Description = "General science trivia" });
        await _context.SaveChangesAsync();

        // Verify data was added to context
        var questionType = _context.QuestionTypes.FirstOrDefault(qt => qt.Id == 6);
        var questionDifficulty = _context.QuestionDifficulties.FirstOrDefault(qd => qd.Id == 6);
        var quizCategory = _context.QuizCategories.FirstOrDefault(qc => qc.Id == 6);

        var request = new ExportQuizQuestionsRequestDto
        {
            QuizName = "Sample Quiz",
            Questions = new List<QuestionsListRequestDto>
        {
            new QuestionsListRequestDto
            {
                QueText = "Capital of France?",
                QueTypeId = 6,
                QueDifficultyId = 6,
                CategoryId = 6,
                QueOptionsAns = new List<QueOptionsAndAnswersDto>
                {
                    new() { Key = "option1", Value = "Paris" },
                    new() { Key = "option2", Value = "Lyon" },
                    new() { Key = Constants.QUESTION_KEY_ANSWER, Value = "Paris" }
                }
            }
        }
        };

        // Act
        var csv = await _quizService.ExportQuestionsToCsv(request);

        // Log the CSV output for debugging
        Console.WriteLine($"CSV Output:\n{csv}");

        // Assert
        var csvLines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.True(csvLines.Length >= 2, "CSV should contain at least header and one data row");

        var dataRow = csvLines[1];
        var expectedRow = "Capital of France?,Multiple Choice,Easy,Science,Paris,Lyon,,,Paris";
        Assert.Equal(expectedRow, dataRow.Trim(), StringComparer.Ordinal);

        Assert.Contains("Capital of France?", dataRow);
        Assert.Contains("Paris", dataRow);
        Assert.Contains("Lyon", dataRow);
    }
    #endregion
}
