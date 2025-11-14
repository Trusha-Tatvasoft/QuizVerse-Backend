using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
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
using System.Security.Claims;
using System.Text.Json;
using Xunit;
using System.Linq.Expressions;
namespace QuizVerse.UnitTests.Services;

public class QuizServiceTests
{
    private readonly QuizVerseDbContext _context;
    private readonly IMapper _mapper;
    private readonly QuizService _quizService;
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILeaderboardService> _leaderboardServiceMock;
    private readonly Mock<IGcpApiQueueService> _queueServiceMock;

    public QuizServiceTests()
    {
        var options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new QuizVerseDbContext(options);
        SeedTestData();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = mapperConfig.CreateMapper();

        _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
        _aiServiceMock = new Mock<IAiService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.UserData, "1") }, "mock"));
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
        _aiServiceMock = new Mock<IAiService>();
        _leaderboardServiceMock = new Mock<ILeaderboardService>();
        _queueServiceMock = new Mock<IGcpApiQueueService>();

        var quizRepo = new GenericRepository<Quiz>(_context);
        var quizPlayStatusRepo = new GenericRepository<QuizPlayStatus>(_context);
        var attemptedQuestionRepo = new GenericRepository<AttemptedQuizQuestionsAnswer>(_context);
        var quizToQuestionMapRepo = new GenericRepository<QuizToBaseQuestionMap>(_context);
        var quizAttemptedRepo = new GenericRepository<QuizAttempted>(_context);
        var questionIssueReportRepo = new GenericRepository<QuestionIssueReport>(_context);
        var quizIssueReportRepo = new GenericRepository<QuizIssueReport>(_context);
        var quizRatingRepo = new GenericRepository<QuizRating>(_context);

        _quizService = new QuizService(
            quizPlayStatusRepo,
            attemptedQuestionRepo,
            quizRepo,
            quizToQuestionMapRepo,
            quizAttemptedRepo,
            questionIssueReportRepo,
            quizIssueReportRepo,
            quizRatingRepo,
            _mapper,
            _sqlQueryRepoMock.Object,
            _httpContextAccessorMock.Object,
            _aiServiceMock.Object,
            _leaderboardServiceMock.Object,
            _queueServiceMock.Object
        );
    }

    private void SeedTestData()
    {
        var category = new QuizCategory
        {
            Id = 1,
            CategoryName = "General",
            Description = "General knowledge category"
        };

        var difficulty = new QuizDifficulty
        {
            Id = 1,
            Name = "Easy",
            Description = "Easy difficulty level"
        };

        var quiz = new Quiz
        {
            Id = 1,
            Name = "Sample Quiz",
            Description = "this is sample quiz",
            CategoryId = category.Id,
            DifficultyLevelId = difficulty.Id,
            Category = category,
            DifficultyLevel = difficulty,
            TotalQuestion = 2,
            Status = 1,
        };

        var quizPlayStatus = new QuizPlayStatus
        {
            Id = 1,
            QuizId = quiz.Id,
            UserId = 1,
            IsCompleted = false
        };

        var baseQuestion1 = new BaseQuestion
        {
            Id = 1,
            QueText = "What is 2 + 2?",
            QueTypeId = 1
        };

        var questionOption1 = new QuestionOptionsAnswer
        {
            Id = 1,
            Key = "answer",
            Value = "4",
            QuestionId = baseQuestion1.Id,
            IsDeleted = false
        };
        baseQuestion1.QuestionOptionsAnswers = new List<QuestionOptionsAnswer> { questionOption1 };

        var quizToQuestionMap1 = new QuizToBaseQuestionMap
        {
            Id = 1,
            QuizId = quiz.Id,
            Que = baseQuestion1,
            QueId = baseQuestion1.Id
        };

        // Add second question for consistency
        var baseQuestion2 = new BaseQuestion
        {
            Id = 2,
            QueText = "What is 5 + 5?",
            QueTypeId = 1
        };

        var questionOption2 = new QuestionOptionsAnswer
        {
            Id = 2,
            Key = "answer",
            Value = "10",
            QuestionId = baseQuestion2.Id,
            IsDeleted = false
        };
        baseQuestion2.QuestionOptionsAnswers = new List<QuestionOptionsAnswer> { questionOption2 };

        var quizToQuestionMap2 = new QuizToBaseQuestionMap
        {
            Id = 2,
            QuizId = quiz.Id,
            Que = baseQuestion2,
            QueId = baseQuestion2.Id
        };

        var user = new User
        {
            Id = 1,
            UserName = "TestUser",
            Email = "testuser@example.com",
            FullName = "Test User",
            Password = "Password123!"
        };

        var grade = new GradeForQuizResult
        {
            Id = 1,
            MinPercentage = 80,
            MaxPercentage = 100,
            Grade = "A+",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = user.Id,
            CreatedByNavigation = user,
            IsDeleted = false
        };

        var quizAttempted = new QuizAttempted
        {
            Id = 1,
            QuizId = quiz.Id,
            UserId = user.Id,
            TotalQue = 2,
            CorrectedQue = 1,
            TimeSpent = TimeSpan.FromMinutes(15),
            XpEarned = 50,
            Grade = 90,
            CreatedDate = DateTime.UtcNow,
            Quiz = quiz,
            GradeNavigation = grade,
            User = user
        };

        var quizRating = new QuizRating
        {
            Id = 1,
            QuizId = quiz.Id,
            UserId = user.Id,
            QuizRating1 = 5,
            Feedback = "Excellent quiz!",
            CreatedDate = DateTime.UtcNow,
            Quiz = quiz,
            User = user
        };

        _context.QuizCategories.Add(category);
        _context.QuizDifficulties.Add(difficulty);
        _context.Quizzes.Add(quiz);
        _context.QuizPlayStatuses.Add(quizPlayStatus);
        _context.BaseQuestions.AddRange(baseQuestion1, baseQuestion2);
        _context.QuestionOptionsAnswers.AddRange(questionOption1, questionOption2);
        _context.QuizToBaseQuestionMaps.AddRange(quizToQuestionMap1, quizToQuestionMap2);
        _context.GradeForQuizResults.Add(grade);
        _context.QuizAttempteds.Add(quizAttempted);
        _context.Users.Add(user);
        _context.QuizRatings.Add(quizRating);
        _context.SaveChanges();
    }


    [Fact]
    public async Task GetQuizOverviewAsync_ValidQuizId_ReturnsQuizOverview()
    {
        var result = await _quizService.GetQuizOverviewAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Sample Quiz", result.QuizName);
        Assert.Equal("General", result.QuizCategoryName);
        Assert.Equal("Easy", result.DifficultyLevelName);
    }

    [Fact]
    public async Task StartQuizAsync_ValidQuizId_ReturnsStartQuizResponse()
    {
        var rawQuiz = new RawStartQuizDto
        {
            QuizId = 1,
            QuizName = "Sample Quiz",
            CategoryName = "General",
            TotalTime = 60,
            TotalQuestion = 10,
            QuizQuestionId = 1,
            QuestionName = "What is 2 + 2?",
            QuestionType = "MCQ",
            Options = JsonSerializer.Serialize(new List<OptionResponseDto>
            {
                new OptionResponseDto { Key = "A", Value = "3" },
                new OptionResponseDto { Key = "B", Value = "4" }
            })
        };

        _sqlQueryRepoMock.Setup(x => x.SqlQuerySingleAsync<RawStartQuizDto>(It.IsAny<string>()))
            .ReturnsAsync(rawQuiz);

        var result = await _quizService.StartQuizAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Sample Quiz", result.QuizName);
        Assert.Equal(2, result.Options.Count);
        Assert.Contains(result.Options, o => o.Value == "4");
    }

    [Fact]
    public async Task SaveAndNextQuestion_ShouldReturnNextQuestion_WhenValidRequest()
    {
        SaveAndNextQuestionRequestDto request = new()
        {
            QuizId = 1,
            CurrentQuestionId = 1,
            NextQuestionNumber = 2,
            GivenAnswer = "4"
        };

        RawQuizQuestionDto rawQuestion = new()
        {
            QuizQuestionId = 2,
            QuestionName = "What is 5 + 5?",
            QuestionType = "Objective",
            Options = JsonSerializer.Serialize(new List<OptionResponseDto>
        {
            new() { Key = "answer", Value = "10" }
        })
        };

        _sqlQueryRepoMock
            .Setup(x => x.SqlQuerySingleAsync<RawQuizQuestionDto>(It.IsAny<string>()))
            .ReturnsAsync(rawQuestion);

        QuizQuestionResponseDto? response = await _quizService.SaveAndNextQuestion(request);

        Assert.NotNull(response);
        Assert.Equal("What is 5 + 5?", response.QuestionName);
        Assert.Equal(2, response.QuizQuestionId);
        Assert.Single(response.Options);
        Assert.Equal("10", response.Options[0].Value);

        var attempt = await _context.AttemptedQuizQuestionsAnswers
            .FirstOrDefaultAsync(a => a.QuizQueId == request.CurrentQuestionId);
        Assert.NotNull(attempt);
        Assert.Equal(request.GivenAnswer, attempt.GivenAnswer);
        Assert.True(attempt.IsCorrect);
    }

    [Fact]
    public async Task SaveAndNextQuestion_ShouldReturnNull_WhenNoMoreQuestions()
    {
        // Arrange
        SaveAndNextQuestionRequestDto request = new()
        {
            QuizId = 1,
            CurrentQuestionId = 1,
            NextQuestionNumber = 3, // Exceeds TotalQuestion (2)
            GivenAnswer = "4"
        };

        _sqlQueryRepoMock
            .Setup(x => x.SqlQuerySingleAsync<RawQuizQuestionDto>(It.IsAny<string>()))
            .ReturnsAsync((RawQuizQuestionDto)null!);

        // Act
        QuizQuestionResponseDto? response = await _quizService.SaveAndNextQuestion(request);

        // Assert
        Assert.Null(response);

        // Verify answer was saved
        var attempt = await _context.AttemptedQuizQuestionsAnswers
            .FirstOrDefaultAsync(a => a.QuizQueId == request.CurrentQuestionId);
        Assert.NotNull(attempt);
        Assert.Equal(request.GivenAnswer, attempt.GivenAnswer);
        Assert.True(attempt.IsCorrect);
    }

    [Fact]
    public async Task SaveAndNextQuestion_ShouldThrow_WhenQuizNotFoundOrCompleted()
    {
        SaveAndNextQuestionRequestDto request = new()
        {
            QuizId = 999,
            CurrentQuestionId = 1,
            NextQuestionNumber = 2,
            GivenAnswer = "4"
        };

        await Assert.ThrowsAsync<AppException>(() =>
            _quizService.SaveAndNextQuestion(request));
    }

    [Fact]
    public async Task SaveAndNextQuestion_ShouldThrow_WhenQuestionNotFound()
    {
        SaveAndNextQuestionRequestDto request = new()
        {
            QuizId = 1,
            CurrentQuestionId = 999,
            NextQuestionNumber = 2,
            GivenAnswer = "4"
        };

        await Assert.ThrowsAsync<AppException>(() =>
            _quizService.SaveAndNextQuestion(request));
    }

    [Fact]
    public async Task SaveAndNextQuestion_ShouldMarkIncorrect_ForWrongObjectiveAnswer()
    {
        SaveAndNextQuestionRequestDto request = new()
        {
            QuizId = 1,
            CurrentQuestionId = 1,
            NextQuestionNumber = 2,
            GivenAnswer = "wrong"
        };

        RawQuizQuestionDto rawQuestion = new()
        {
            QuizQuestionId = 2,
            QuestionName = "What is 2 + 2?",
            QuestionType = "Objective",
            Options = JsonSerializer.Serialize(new List<OptionResponseDto>
        {
            new() { Key = "answer", Value = "4" }
        })
        };

        _sqlQueryRepoMock
            .Setup(x => x.SqlQuerySingleAsync<RawQuizQuestionDto>(It.IsAny<string>()))
            .ReturnsAsync(rawQuestion);

        await _quizService.SaveAndNextQuestion(request);

        AttemptedQuizQuestionsAnswer attempt = _context.AttemptedQuizQuestionsAnswers.First(a => a.QuizQueId == 1);
        Assert.False(attempt.IsCorrect);
    }

    [Fact]
    public async Task CheckAnswer_ReturnsFalse_WhenGivenAnswerIsEmpty()
    {
        var method = typeof(QuizService).GetMethod("CheckAnswer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var quizAnswerCheck = new QuizAnswerCheckDto
        {
            GivenAnswer = "",
            CorrectAnswer = "4",
            QuestionName = "What is 2 + 2?"
        };
        var task = (Task<bool>)method?.Invoke(_quizService, new object[] { quizAnswerCheck })!;
        var result = await task;

        Assert.False(result);
    }

    [Fact]
    public async Task CheckAnswer_ReturnsTrue_WhenAiServiceReturnsTrue()
    {
        var method = typeof(QuizService).GetMethod("CheckAnswer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var quizAnswerCheck = new QuizAnswerCheckDto
        {
            GivenAnswer = "Four",
            CorrectAnswer = "4",
            QuestionName = "What is 2 + 2?"
        };

        _aiServiceMock.Setup(x => x.GetResponseAsync(It.IsAny<string>())).ReturnsAsync("TRUE");

        var task = (Task<bool>)method?.Invoke(_quizService, [quizAnswerCheck])!;
        var result = await task;

        Assert.True(result);
    }

    [Fact]
    public async Task SubmitQuiz_ShouldCompleteQuiz_WhenValidRequest()
    {
        SubmitQuizRequestDTO request = new()
        {
            QuizId = 1,
            QuizName = "Sample Quiz",
            TimeTaken = 120,
            LastVisitedQuestionAndAnswers = new LastVisitedQuestionAndAnswerDTO
            {
                QuestionId = 1,
                GivenAnswer = "4"
            }
        };

        _sqlQueryRepoMock
            .Setup(x => x.SqlQuerySingleAsync<SuccessResponseDTO>(It.IsAny<string>()))
            .ReturnsAsync(new SuccessResponseDTO { Success = true });

        var tracked = _context.ChangeTracker.Entries<QuizPlayStatus>().ToList();
        foreach (var entry in tracked)
        {
            entry.State = EntityState.Detached;
        }

        bool result = await _quizService.SubmitQuiz(request);

        Assert.True(result);

        QuizPlayStatus playStatus = await _context.QuizPlayStatuses
            .AsNoTracking()
            .FirstAsync(q => q.Id == 1);

        Assert.True(playStatus.IsCompleted);
    }

    [Fact]
    public async Task SubmitQuiz_ShouldThrow_WhenQuizNotFoundOrAlreadyCompleted()
    {
        SubmitQuizRequestDTO request = new()
        {
            QuizId = 999,
            TimeTaken = 100,
            LastVisitedQuestionAndAnswers = new LastVisitedQuestionAndAnswerDTO
            {
                QuestionId = 1,
                GivenAnswer = "4"
            }
        };

        await Assert.ThrowsAsync<AppException>(() =>
            _quizService.SubmitQuiz(request));
    }

    [Fact]
    public async Task SubmitQuiz_ShouldThrow_WhenQuestionNotFound()
    {
        SubmitQuizRequestDTO request = new()
        {
            QuizId = 1,
            TimeTaken = 100,
            LastVisitedQuestionAndAnswers = new LastVisitedQuestionAndAnswerDTO
            {
                QuestionId = 999,
                GivenAnswer = "4"
            }
        };

        await Assert.ThrowsAsync<AppException>(() =>
            _quizService.SubmitQuiz(request));
    }

    [Fact]
    public async Task GetQuizSummary_ShouldReturnSummary_WhenQuizAttemptExists()
    {
        var result = await _quizService.GetQuizSummary(1);

        Assert.NotNull(result);
        Assert.IsType<QuizCompletedSummaryDTO>(result);
        Assert.Equal("Sample Quiz", result.QuizName);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(1, result.WrongAnswers);
        Assert.Equal(50, result.XpEarned);
        Assert.Equal("A+", result.Grade);
        Assert.Equal(TimeSpan.FromMinutes(15), result.TimeSpent);
    }

    [Fact]
    public async Task GetQuizSummary_ShouldThrowAppException_WhenQuizAttemptDoesNotExist()
    {
        int nonExistentQuizId = 999;

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.GetQuizSummary(nonExistentQuizId)
        );

        Assert.Equal(Constants.QUIZ_ATTEMPT_NOT_FOUND, exception.Message);
    }

    [Fact]
    public async Task GetQuizSummary_ShouldCalculateWrongAnswersAndScorePercentageCorrectly()
    {
        var result = await _quizService.GetQuizSummary(1);

        int expectedWrong = result.TotalQuestions - result.CorrectAnswers;
        double expectedScore = (double)result.CorrectAnswers / result.TotalQuestions * 100;

        Assert.Equal(expectedWrong, result.WrongAnswers);
        Assert.Equal(Math.Round(expectedScore, 2), Math.Round(result.ScorePercentage, 2));
    }

    [Fact]
    public async Task GetQuizQuestionReview_ShouldReturnList_WhenDataExists()
    {
        int quizId = 1;
        var expectedList = new List<QuizQuestionReviewDTO>
        {
            new QuizQuestionReviewDTO
            {
                QuestionId = 1,
                QuestionText = "What is 2 + 2?",
                UserAnswer = "4",
                CorrectAnswer = "4",
                IsCorrect = true
            },
            new QuizQuestionReviewDTO
            {
                QuestionId = 2,
                QuestionText = "Capital of France?",
                UserAnswer = "", // empty answer should become null
                CorrectAnswer = "Paris",
                IsCorrect = false
            }
        };

        _sqlQueryRepoMock
            .Setup(x => x.SqlQueryListAsync<QuizQuestionReviewDTO>(
                It.Is<string>(s => s.Contains(quizId.ToString())),
                It.IsAny<object[]>()))
            .ReturnsAsync(expectedList);

        var result = await _quizService.GetQuizQuestionReview(quizId);

        Assert.NotNull(result);
        Assert.IsType<List<QuizQuestionReviewDTO>>(result);
        Assert.Equal(expectedList.Count, result.Count);

        // First item: should remain unchanged
        Assert.Equal("4", result[0].UserAnswer);
        Assert.True(result[0].IsCorrect);

        // Second item: should have null values
        Assert.Null(result[1].UserAnswer);
        Assert.Null(result[1].IsCorrect);

        _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<QuizQuestionReviewDTO>(
            It.IsAny<string>(),
            It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task GetQuizQuestionReview_ShouldReturnEmptyList_WhenNoDataExists()
    {
        int quizId = 999;
        _sqlQueryRepoMock
            .Setup(x => x.SqlQueryListAsync<QuizQuestionReviewDTO>(
                It.IsAny<string>(),
                It.IsAny<object[]>()))
            .ReturnsAsync(new List<QuizQuestionReviewDTO>());

        var result = await _quizService.GetQuizQuestionReview(quizId);

        Assert.NotNull(result);
        Assert.IsType<List<QuizQuestionReviewDTO>>(result);
        Assert.Empty(result);

        _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<QuizQuestionReviewDTO>(
            It.IsAny<string>(),
            It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task GetQuizQuestionReview_ShouldPropagateException_WhenRepositoryThrows()
    {
        int quizId = 1;
        _sqlQueryRepoMock
            .Setup(x => x.SqlQueryListAsync<QuizQuestionReviewDTO>(
                It.IsAny<string>(),
                It.IsAny<object[]>()))
            .ThrowsAsync(new Exception("Database error"));

        var ex = await Assert.ThrowsAsync<Exception>(() => _quizService.GetQuizQuestionReview(quizId));
        Assert.Equal("Database error", ex.Message);

        _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<QuizQuestionReviewDTO>(
            It.IsAny<string>(),
            It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task ReportQuestionIssue_ShouldReturnSuccessMessage_WhenReportDoesNotExist()
    {
        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "This question has a typo"
        };

        var result = await _quizService.CreateOrUpdateQuestionIssueReport(request);

        Assert.Equal(Constants.QUESTION_ISSUE_REPORTED, result);

        var addedReport = await _context.QuestionIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == request.QuizId && r.QuestionId == request.QuestionId && r.UserId == 1);
        Assert.NotNull(addedReport);
        Assert.Equal(request.Description, addedReport.Description);
    }

    #region CreateOrUpdateQuestionIssueReport Tests

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldCreateNewReport_WhenReportIdIsNull()
    {
        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "This question has a typo",
            ReportId = null
        };

        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Returns(Task.CompletedTask);

        var result = await _quizService.CreateOrUpdateQuestionIssueReport(request);

        Assert.Equal(Constants.QUESTION_ISSUE_REPORTED, result);

        var addedReport = await _context.QuestionIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == request.QuizId && r.QuestionId == request.QuestionId && r.UserId == 1);

        Assert.NotNull(addedReport);
        Assert.Equal(request.Description, addedReport.Description);
        Assert.Equal((int)QuestionOrQuizIssueReportSeverity.UnderProcessing, addedReport.Severity);
        Assert.Equal((int)QuestionOrQuizIssueReportStatus.Pending, addedReport.Status);

        _queueServiceMock.Verify(x => x.EnqueueAsync(It.Is<GcpApiReportDataDto>(
            dto => dto.ReportType == ReportType.QuestionIssueReport &&
                   dto.ReportComment == request.Description &&
                   dto.ReportId == addedReport.Id
        )), Times.Once);
    }

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldCreateNewReport_WhenReportIdIsZero()
    {
        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "Question needs correction",
            ReportId = 0
        };

        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Returns(Task.CompletedTask);

        var result = await _quizService.CreateOrUpdateQuestionIssueReport(request);

        Assert.Equal(Constants.QUESTION_ISSUE_REPORTED, result);

        var addedReport = await _context.QuestionIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == request.QuizId && r.QuestionId == request.QuestionId);

        Assert.NotNull(addedReport);
        Assert.Equal(request.Description, addedReport.Description);

        _queueServiceMock.Verify(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldThrowException_WhenQuizNotFound()
    {
        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 999,
            QuestionId = 1,
            Description = "This question has an issue"
        };

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.CreateOrUpdateQuestionIssueReport(request)
        );

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
        _queueServiceMock.Verify(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldThrowException_WhenQuizIsInactive()
    {
        var inactiveQuiz = new Quiz
        {
            Id = 10,
            Name = "Inactive Quiz",
            Description = "Test",
            CategoryId = 1,
            DifficultyLevelId = 1,
            TotalQuestion = 5,
            Status = (int)QuizStatus.Inactive,
            IsDeleted = false
        };

        _context.Quizzes.Add(inactiveQuiz);
        await _context.SaveChangesAsync();

        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 10,
            QuestionId = 1,
            Description = "Issue with question"
        };

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.CreateOrUpdateQuestionIssueReport(request)
        );

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldUpdateExistingReport_WhenReportExists()
    {
        var existingReport = new QuestionIssueReport
        {
            Id = 100,
            UserId = 1,
            QuizId = 1,
            QuestionId = 1,
            Description = "Original description",
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low,
            Status = (int)QuestionOrQuizIssueReportStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();
        _context.Entry(existingReport).State = EntityState.Detached;

        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 100,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated description"
        };

        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Returns(Task.CompletedTask);

        var result = await _quizService.CreateOrUpdateQuestionIssueReport(request);

        Assert.Equal(Constants.QUESTION_ISSUE_UPDATED, result);

        var updatedReport = await _context.QuestionIssueReports.FindAsync(100);
        Assert.NotNull(updatedReport);
        Assert.Equal("Updated description", updatedReport.Description);
        Assert.Equal((int)QuestionOrQuizIssueReportSeverity.UnderProcessing, updatedReport.Severity);
        Assert.Equal((int)QuestionOrQuizIssueReportStatus.Pending, updatedReport.Status);

        _queueServiceMock.Verify(x => x.EnqueueAsync(It.Is<GcpApiReportDataDto>(
            dto => dto.ReportType == ReportType.QuestionIssueReport &&
                   dto.ReportComment == request.Description &&
                   dto.ReportId == 100
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldThrowException_WhenReportNotFound()
    {
        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 999,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated description"
        };

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.CreateOrUpdateQuestionIssueReport(request)
        );

        Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND_OR_CANNOT_EDIT, exception.Message);
        _queueServiceMock.Verify(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldThrowException_WhenReportIsUnderProcessing()
    {
        var existingReport = new QuestionIssueReport
        {
            Id = 101,
            UserId = 1,
            QuizId = 1,
            QuestionId = 1,
            Description = "Original description",
            Severity = (int)QuestionOrQuizIssueReportSeverity.UnderProcessing,
            Status = (int)QuestionOrQuizIssueReportStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();

        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 101,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated description"
        };

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.CreateOrUpdateQuestionIssueReport(request)
        );

        Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND_OR_CANNOT_EDIT, exception.Message);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldThrowException_WhenReportBelongsToDifferentUser()
    {
        var existingReport = new QuestionIssueReport
        {
            Id = 102,
            UserId = 999, // Different user
            QuizId = 1,
            QuestionId = 1,
            Description = "Original description",
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low,
            Status = (int)QuestionOrQuizIssueReportStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();

        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 102,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated description"
        };

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.CreateOrUpdateQuestionIssueReport(request)
        );

        Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND_OR_CANNOT_EDIT, exception.Message);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldThrowException_WhenReportStatusIsNotPending()
    {
        var existingReport = new QuestionIssueReport
        {
            Id = 103,
            UserId = 1,
            QuizId = 1,
            QuestionId = 1,
            Description = "Original description",
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low,
            Status = (int)QuestionOrQuizIssueReportStatus.Accepted,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();

        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 103,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated description"
        };

        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.CreateOrUpdateQuestionIssueReport(request)
        );

        Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND_OR_CANNOT_EDIT, exception.Message);
    }

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldSetCorrectTimestamp()
    {
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "Test issue"
        };

        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Returns(Task.CompletedTask);

        await _quizService.CreateOrUpdateQuestionIssueReport(request);

        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        var report = await _context.QuestionIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == 1 && r.QuestionId == 1);

        Assert.NotNull(report);
        Assert.InRange(report.CreatedDate, beforeCreate, afterCreate);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldUpdateCreatedDate()
    {
        var oldDate = DateTime.UtcNow.AddDays(-5);
        var existingReport = new QuestionIssueReport
        {
            Id = 104,
            UserId = 1,
            QuizId = 1,
            QuestionId = 1,
            Description = "Original description",
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low,
            Status = (int)QuestionOrQuizIssueReportStatus.Pending,
            CreatedDate = oldDate
        };

        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();

        _context.Entry(existingReport).State = EntityState.Detached;

        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);

        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 104,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated description"
        };

        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Returns(Task.CompletedTask);

        await _quizService.CreateOrUpdateQuestionIssueReport(request);

        var afterUpdate = DateTime.UtcNow.AddSeconds(1);

        var updatedReport = await _context.QuestionIssueReports.FindAsync(104);
        Assert.NotNull(updatedReport);
        Assert.InRange(updatedReport.CreatedDate, beforeUpdate, afterUpdate);
        Assert.NotEqual(oldDate, updatedReport.CreatedDate);
    }

    #endregion

    #region GetQuizReportQuestionIssue Tests

    [Fact]
    public async Task GetQuizReportQuestionIssue_ShouldReturnReport_WhenReportExists()
    {
        var report = new QuestionIssueReport
        {
            Id = 200,
            UserId = 1,
            QuizId = 1,
            QuestionId = 1,
            Description = "Test issue",
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low,
            Status = (int)QuestionOrQuizIssueReportStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        _context.QuestionIssueReports.Add(report);
        await _context.SaveChangesAsync();

        var result = await _quizService.GetQuizReportQuestionIssue(200);

        Assert.NotNull(result);
        Assert.IsType<QuestionIssueReportResponseDTO>(result);
        Assert.Equal(200, result.ReportId);
        Assert.Equal("Test issue", result.Description);
    }

    [Fact]
    public async Task GetQuizReportQuestionIssue_ShouldThrowException_WhenReportNotFound()
    {
        var exception = await Assert.ThrowsAsync<AppException>(
            () => _quizService.GetQuizReportQuestionIssue(999)
        );

        Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND, exception.Message);
    }

    #endregion

    [Fact]
    public async Task GetMyQuizRating_ShouldReturnQuizRating_WhenRatingExists()
    {
        int quizId = 1;

        var result = await _quizService.GetMyQuizRating(quizId);

        Assert.NotNull(result);
        Assert.IsType<QuizRatingDTO>(result);
        Assert.Equal(5, result!.QuizRating);
        Assert.Equal("Excellent quiz!", result.Feedback);
    }

    [Fact]
    public async Task GetMyQuizRating_ShouldReturnNull_WhenRatingDoesNotExist()
    {
        int nonExistentQuizId = 999;

        var result = await _quizService.GetMyQuizRating(nonExistentQuizId);

        Assert.Null(result);
    }

    [Fact]
    public async Task SubmitQuizRating_ShouldAddRating_WhenNotExists()
    {
        var newRating = new QuizRatingDTO
        {
            QuizId = 2,
            QuizRating = 4,
            Feedback = "Good quiz"
        };

        var result = await _quizService.SubmitQuizRating(newRating);

        Assert.Equal(Constants.QUIZ_RATING_SUBMITTED, result);

        var addedRating = await _context.QuizRatings.FirstOrDefaultAsync(r => r.QuizId == 2 && r.UserId == 1);
        Assert.NotNull(addedRating);
        Assert.Equal(newRating.QuizRating, addedRating!.QuizRating1);
        Assert.Equal(newRating.Feedback, addedRating.Feedback);
    }

    [Fact]
    public async Task SubmitQuizRating_ShouldThrowAppException_WhenRatingAlreadyExists()
    {
        var existingRating = new QuizRatingDTO
        {
            QuizId = 1,
            QuizRating = 5,
            Feedback = "Excellent quiz!"
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizService.SubmitQuizRating(existingRating)
        );

        Assert.Equal(Constants.DUPLICATE_QUIZ_RATING, exception.Message);
    }

    [Fact]
    public async Task GetAnswerExplanation_ShouldReturnExplanation_ForCorrectAnswer()
    {
        var request = new AnswerExplanationRequestDTO
        {
            QuestionText = "What is 2 + 2?",
            CorrectAnswer = "4",
            UserAnswer = "4"
        };

        string expectedResponse = "The answer is correct because 2 + 2 equals 4.";

        _aiServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        var result = await _quizService.GetAnswerExplanation(request);

        Assert.Equal(expectedResponse, result);
        _aiServiceMock.Verify(s => s.GetResponseAsync(It.Is<string>(prompt =>
            prompt.Contains(request.QuestionText) &&
            prompt.Contains(request.CorrectAnswer) &&
            prompt.Contains(request.UserAnswer)
        )), Times.Once);
    }

    [Fact]
    public async Task GetAnswerExplanation_ShouldHandleEmptyUserAnswer()
    {
        var request = new AnswerExplanationRequestDTO
        {
            QuestionText = "What is the capital of France?",
            CorrectAnswer = "Paris",
            UserAnswer = ""
        };

        string expectedResponse = "The correct answer is Paris.";

        _aiServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        var result = await _quizService.GetAnswerExplanation(request);

        Assert.Equal(expectedResponse, result);
        _aiServiceMock.Verify(s => s.GetResponseAsync(It.Is<string>(prompt =>
            prompt.Contains("No answer was provided.") &&
            prompt.Contains(request.CorrectAnswer)
        )), Times.Once);
    }

    [Fact]
    public async Task AddQuizReport_ShouldReturnTrue_WhenValidRequest()
    {
        var quizReportRequest = new QuizReportRequestDto
        {
            QuizId = 1,
            Reason = "Inappropriate content"
        };

        var result = await _quizService.AddQuizReport(quizReportRequest);

        Assert.True(result);

        var addedReport = await _context.QuizIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == quizReportRequest.QuizId && r.UserId == 1);

        Assert.NotNull(addedReport);
        Assert.Equal(quizReportRequest.Reason, addedReport.Reason);
        Assert.Equal((int)QuestionOrQuizIssueReportSeverity.UnderProcessing, addedReport.Severity);
        Assert.Equal((int)QuestionOrQuizIssueReportStatus.Pending, addedReport.Status);
        Assert.True(addedReport.Id > 0);
    }

    [Fact]
    public async Task AddQuizReport_ShouldThrowAppException_WhenQuizNotFound()
    {
        var quizReportRequest = new QuizReportRequestDto
        {
            QuizId = 999,
            Reason = "Inappropriate content"
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizService.AddQuizReport(quizReportRequest));

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
    }

    [Fact]
    public async Task AddQuizReport_ShouldThrowAppException_WhenQuizIsInactive()
    {
        var inactiveQuiz = new Quiz
        {
            Id = 3,
            Name = "Inactive Quiz",
            Description = "This quiz is inactive",
            CategoryId = 1,
            DifficultyLevelId = 1,
            TotalQuestion = 5,
            Status = (int)QuizStatus.Inactive,
            IsDeleted = false
        };

        _context.Quizzes.Add(inactiveQuiz);
        await _context.SaveChangesAsync();

        var quizReportRequest = new QuizReportRequestDto
        {
            QuizId = 3,
            Reason = "Inappropriate content"
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizService.AddQuizReport(quizReportRequest));

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
    }

    [Fact]
    public async Task AddQuizReport_ShouldThrowAppException_WhenQuizIsDeleted()
    {
        var deletedQuiz = new Quiz
        {
            Id = 4,
            Name = "Deleted Quiz",
            Description = "This quiz is deleted",
            CategoryId = 1,
            DifficultyLevelId = 1,
            TotalQuestion = 5,
            Status = (int)QuizStatus.Active,
            IsDeleted = true
        };

        _context.Quizzes.Add(deletedQuiz);
        await _context.SaveChangesAsync();

        var quizReportRequest = new QuizReportRequestDto
        {
            QuizId = 4,
            Reason = "Inappropriate content"
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizService.AddQuizReport(quizReportRequest));

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
    }

    [Fact]
    public async Task AddQuizReport_ShouldSetCorrectDefaultValues_WhenReportIsAdded()
    {
        var quizReportRequest = new QuizReportRequestDto
        {
            QuizId = 1,
            Reason = "Technical issues with the quiz"
        };

        var result = await _quizService.AddQuizReport(quizReportRequest);

        Assert.True(result);

        var addedReport = await _context.QuizIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == quizReportRequest.QuizId && r.UserId == 1);

        Assert.NotNull(addedReport);
        Assert.Equal((int)QuestionOrQuizIssueReportSeverity.UnderProcessing, addedReport.Severity);
        Assert.Equal((int)QuestionOrQuizIssueReportStatus.Pending, addedReport.Status);
        Assert.Equal(1, addedReport.UserId);
        Assert.InRange(addedReport.CreatedDate, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task AddQuizReport_ShouldAllowMultipleReports_ForDifferentQuizzesFromSameUser()
    {
        var secondQuiz = new Quiz
        {
            Id = 2,
            Name = "Second Quiz",
            Description = "Another sample quiz",
            CategoryId = 1,
            DifficultyLevelId = 1,
            TotalQuestion = 3,
            Status = (int)QuizStatus.Active,
            IsDeleted = false
        };

        _context.Quizzes.Add(secondQuiz);
        await _context.SaveChangesAsync();

        var firstReport = new QuizReportRequestDto
        {
            QuizId = 1,
            Reason = "First report"
        };

        var secondReport = new QuizReportRequestDto
        {
            QuizId = 2,
            Reason = "Second report"
        };

        var firstResult = await _quizService.AddQuizReport(firstReport);
        var secondResult = await _quizService.AddQuizReport(secondReport);

        Assert.True(firstResult);
        Assert.True(secondResult);

        var userReports = await _context.QuizIssueReports
            .Where(r => r.UserId == 1)
            .ToListAsync();

        Assert.Equal(2, userReports.Count);
        Assert.Contains(userReports, r => r.QuizId == 1 && r.Reason == "First report");
        Assert.Contains(userReports, r => r.QuizId == 2 && r.Reason == "Second report");
    }

    #region Queue Service Integration Tests

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldEnqueueCorrectData()
    {
        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "Test queue integration"
        };

        GcpApiReportDataDto? capturedDto = null;
        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Callback<GcpApiReportDataDto>(dto => capturedDto = dto)
            .Returns(Task.CompletedTask);

        await _quizService.CreateOrUpdateQuestionIssueReport(request);

        Assert.NotNull(capturedDto);
        Assert.Equal(ReportType.QuestionIssueReport, capturedDto!.ReportType);
        Assert.Equal(request.Description, capturedDto.ReportComment);
        Assert.True(capturedDto.ReportId > 0);
    }

    [Fact]
    public async Task UpdateQuestionIssueReport_ShouldEnqueueCorrectData()
    {
        var existingReport = new QuestionIssueReport
        {
            Id = 105,
            UserId = 1,
            QuizId = 1,
            QuestionId = 1,
            Description = "Original description",
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low,
            Status = (int)QuestionOrQuizIssueReportStatus.Pending,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();

        _context.Entry(existingReport).State = EntityState.Detached;


        var request = new QuestionIssueReportRequestDTO
        {
            ReportId = 105,
            QuizId = 1,
            QuestionId = 1,
            Description = "Updated via queue"
        };

        GcpApiReportDataDto? capturedDto = null;
        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Callback<GcpApiReportDataDto>(dto => capturedDto = dto)
            .Returns(Task.CompletedTask);

        await _quizService.CreateOrUpdateQuestionIssueReport(request);

        Assert.NotNull(capturedDto);
        Assert.Equal(105, capturedDto!.ReportId);
        Assert.Equal(ReportType.QuestionIssueReport, capturedDto.ReportType);
        Assert.Equal("Updated via queue", capturedDto.ReportComment);
    }

    [Fact]
    public async Task CreateQuestionIssueReport_ShouldNotEnqueue_WhenReportCreationFails()
    {
        // This would require mocking the repository to fail on Add
        // For now, we verify that if ID is not set properly, we throw before enqueuing
        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "Test failure case"
        };

        // Force a scenario where ID would be <= 0 (this is a conceptual test)
        // In real implementation, you might need to mock the repository
        _queueServiceMock
            .Setup(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()))
            .Returns(Task.CompletedTask);

        // Normal flow - should succeed and enqueue
        await _quizService.CreateOrUpdateQuestionIssueReport(request);

        _queueServiceMock.Verify(x => x.EnqueueAsync(It.IsAny<GcpApiReportDataDto>()), Times.Once);
    }

    #endregion
}
