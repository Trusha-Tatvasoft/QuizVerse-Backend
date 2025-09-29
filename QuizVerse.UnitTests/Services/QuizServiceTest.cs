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
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Mappings;
using QuizVerse.Infrastructure.Repository;
using System.Security.Claims;
using System.Text.Json;
using Xunit;
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

        var quizRepo = new GenericRepository<Quiz>(_context);
        var quizPlayStatusRepo = new GenericRepository<QuizPlayStatus>(_context);
        var attemptedQuestionRepo = new GenericRepository<AttemptedQuizQuestionsAnswer>(_context);
        var quizToQuestionMapRepo = new GenericRepository<QuizToBaseQuestionMap>(_context);
        var quizAttemptedRepo = new GenericRepository<QuizAttempted>(_context);
        var questionIssueReportRepo = new GenericRepository<QuestionIssueReport>(_context);
        var quizRatingRepo = new GenericRepository<QuizRating>(_context);

        _quizService = new QuizService(
            quizPlayStatusRepo,
            attemptedQuestionRepo,
            quizRepo,
            quizToQuestionMapRepo,
            quizAttemptedRepo,
            questionIssueReportRepo,
            quizRatingRepo,
            _mapper,
            _sqlQueryRepoMock.Object,
            _httpContextAccessorMock.Object,
            _aiServiceMock.Object,
            _leaderboardServiceMock.Object
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
                UserAnswer = "Paris",
                CorrectAnswer = "Paris",
                IsCorrect = true
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
        Assert.Equal(expectedList[0].QuestionText, result[0].QuestionText);
        Assert.Equal(expectedList[1].UserAnswer, result[1].UserAnswer);

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

        var result = await _quizService.ReportQuestionIssue(request);

        Assert.Equal(Constants.QUESTION_ISSUE_REPORTED, result);

        var addedReport = await _context.QuestionIssueReports
            .FirstOrDefaultAsync(r => r.QuizId == request.QuizId && r.QuestionId == request.QuestionId && r.UserId == 1);
        Assert.NotNull(addedReport);
        Assert.Equal(request.Description, addedReport.Description);
    }

    [Fact]
    public async Task ReportQuestionIssue_ShouldThrowAppException_WhenReportAlreadyExists()
    {
        var existingReport = new QuestionIssueReport
        {
            QuizId = 1,
            QuestionId = 1,
            UserId = 1,
            CreatedBy = 1,
            Description = "Already reported"
        };
        _context.QuestionIssueReports.Add(existingReport);
        await _context.SaveChangesAsync();

        var request = new QuestionIssueReportRequestDTO
        {
            QuizId = 1,
            QuestionId = 1,
            Description = "Duplicate report attempt"
        };

        var exception = await Assert.ThrowsAsync<AppException>(() => _quizService.ReportQuestionIssue(request));
        Assert.Equal(Constants.DUPLICATE_QUESTION_ISSUE_REPORT, exception.Message);
    }

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
        // Arrange
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

        // Act
        var result = await _quizService.GetAnswerExplanation(request);

        // Assert
        Assert.Equal(expectedResponse, result);
        _aiServiceMock.Verify(s => s.GetResponseAsync(It.Is<string>(prompt =>
            prompt.Contains("No answer was provided.") &&
            prompt.Contains(request.CorrectAnswer)
        )), Times.Once);
    }
}
