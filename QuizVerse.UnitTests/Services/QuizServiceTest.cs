using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
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

        var quizRepo = new GenericRepository<Quiz>(_context);
        var quizPlayStatusRepo = new GenericRepository<QuizPlayStatus>(_context);
        var attemptedQuestionRepo = new GenericRepository<AttemptedQuizQuestionsAnswer>(_context);
        var quizToQuestionMapRepo = new GenericRepository<QuizToBaseQuestionMap>(_context);

        _quizService = new QuizService(
            quizPlayStatusRepo,
            attemptedQuestionRepo,
            quizRepo,
            quizToQuestionMapRepo,
            _mapper,
            _sqlQueryRepoMock.Object,
            _httpContextAccessorMock.Object,
            _aiServiceMock.Object
        );
    }

    private void SeedTestData()
    {
        var category = new QuizCategory
        {
            Id = 1,
            CategoryName = "General",
            Description = "General knowledge category"   // FIX
        };

        var difficulty = new QuizDifficulty
        {
            Id = 1,
            Name = "Easy",
            Description = "Easy difficulty level"        // FIX
        };

        var quiz = new Quiz
        {
            Id = 1,
            Name = "Sample Quiz",
            Description = "this is sample quiz",
            CategoryId = category.Id,
            DifficultyLevelId = difficulty.Id,
            Category = category,
            DifficultyLevel = difficulty
        };

        var quizPlayStatus = new QuizPlayStatus
        {
            Id = 1,
            QuizId = quiz.Id,
            UserId = 1,
            IsCompleted = false
        };

        var baseQuestion = new BaseQuestion
        {
            Id = 1,
            QueText = "What is 2 + 2?",
            QueTypeId = 1 // Objective type
        };

        var questionOption = new QuestionOptionsAnswer
        {
            Id = 1,
            Key = "answer",
            Value = "4",
            QuestionId = baseQuestion.Id,
            IsDeleted = false
        };
        baseQuestion.QuestionOptionsAnswers = new List<QuestionOptionsAnswer> { questionOption };

        var quizToQuestionMap = new QuizToBaseQuestionMap
        {
            Id = 1,
            QuizId = quiz.Id,
            Que = baseQuestion,
            QueId = baseQuestion.Id
        };

        _context.QuizCategories.Add(category);
        _context.QuizDifficulties.Add(difficulty);
        _context.Quizzes.Add(quiz);
        _context.QuizPlayStatuses.Add(quizPlayStatus);
        _context.BaseQuestions.Add(baseQuestion);
        _context.QuestionOptionsAnswers.Add(questionOption);
        _context.QuizToBaseQuestionMaps.Add(quizToQuestionMap);
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
    public async Task SaveAndNextQuestion_ObjectiveAnswer_CorrectAnswer_UpdatesAttemptAndReturnsNextQuestion()
    {
        var request = new SaveAndNextQuestionRequestDto
        {
            QuizId = 1,
            CurrentQuestionId = 1,
            GivenAnswer = "4",
            NextQuestionNumber = 2
        };

        var rawNextQuestion = new RawQuizQuestionDto
        {
            QuizQuestionId = 2,
            QuestionName = "Next Question?",
            QuestionType = "MCQ",
            Options = JsonSerializer.Serialize(new List<OptionResponseDto>
            {
                new OptionResponseDto { Key = "A", Value = "Option 1" },
                new OptionResponseDto { Key = "B", Value = "Option 2" }
            })
        };

        _sqlQueryRepoMock.Setup(x => x.SqlQuerySingleAsync<RawQuizQuestionDto>(It.IsAny<string>()))
            .ReturnsAsync(rawNextQuestion);

        var result = await _quizService.SaveAndNextQuestion(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.QuizQuestionId);
        Assert.Equal("Next Question?", result.QuestionName);

        var answerRecord = _context.AttemptedQuizQuestionsAnswers.FirstOrDefault(a => a.QuizPlayStatusId == 1 && a.QuizQueId == 1);
        Assert.NotNull(answerRecord);
        Assert.True(answerRecord.IsCorrect);
        Assert.Equal("4", answerRecord.GivenAnswer);
    }

    [Fact]
    public async Task SaveAndNextQuestion_SubjectiveAnswer_UsesAiService()
    {
        var baseQuestion = _context.BaseQuestions.First();
        baseQuestion.QueTypeId = 3; // Subjective
        _context.SaveChanges();

        var request = new SaveAndNextQuestionRequestDto
        {
            QuizId = 1,
            CurrentQuestionId = 1,
            GivenAnswer = "Four",
            NextQuestionNumber = 2
        };

        _aiServiceMock.Setup(x => x.GetResponseAsync(It.IsAny<string>()))
            .ReturnsAsync("TRUE");

        var rawNextQuestion = new RawQuizQuestionDto
        {
            QuizQuestionId = 2,
            QuestionName = "Next Question?",
            QuestionType = "MCQ",
            Options = JsonSerializer.Serialize(new List<OptionResponseDto>())
        };

        _sqlQueryRepoMock.Setup(x => x.SqlQuerySingleAsync<RawQuizQuestionDto>(It.IsAny<string>()))
            .ReturnsAsync(rawNextQuestion);

        var result = await _quizService.SaveAndNextQuestion(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.QuizQuestionId);

        var answerRecord = _context.AttemptedQuizQuestionsAnswers.FirstOrDefault(a => a.QuizPlayStatusId == 1 && a.QuizQueId == 1);
        Assert.NotNull(answerRecord);
        Assert.True(answerRecord.IsCorrect);
        Assert.Equal("Four", answerRecord.GivenAnswer);
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
        var task = (Task<bool>)method?.Invoke(_quizService, new object[] { quizAnswerCheck }) !;
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

        var task = (Task<bool>)method?.Invoke(_quizService, [quizAnswerCheck]) !;
        var result = await task;

        Assert.True(result);
    }
}
