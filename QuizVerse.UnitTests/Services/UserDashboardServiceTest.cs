using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class UserDashboardServiceTest
{
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock;
    private readonly Mock<IGenericRepository<BattleRequest>> _battleRequestRepoMock;
    private readonly Mock<IGenericRepository<Quiz>> _quizRepoMock;
    private readonly Mock<IGenericRepository<QuizAttempted>> _quizAttemptedRepoMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserDashboardService _service;

    public UserDashboardServiceTest()
    {
        _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
        _battleRequestRepoMock = new Mock<IGenericRepository<BattleRequest>>();
        _quizRepoMock = new Mock<IGenericRepository<Quiz>>();
        _quizAttemptedRepoMock = new Mock<IGenericRepository<QuizAttempted>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _mapperMock = new Mock<IMapper>();

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, "1")], "mock"))
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _service = new UserDashboardService(
            _battleRequestRepoMock.Object,
            _quizRepoMock.Object,
            _quizAttemptedRepoMock.Object,
            _sqlQueryRepoMock.Object,
            _httpContextAccessorMock.Object,
            _mapperMock.Object
        );
    }

    private static List<QuizAttempted> GetSampleData()
    {
        QuizCategory category = new() { CategoryName = "Mathematics", Description = "Category1" };
        QuizDifficulty difficulty = new() { Name = "Hard", Description = "Difficulty1" };

        Quiz quiz1 = new() { Id = 1, Name = "Algebra Quiz", Category = category, DifficultyLevel = difficulty, Description = "Quiz1" };
        Quiz quiz2 = new() { Id = 2, Name = "Geometry Quiz", Category = category, DifficultyLevel = difficulty, Description = "Quiz2" };
        Quiz quiz3 = new() { Id = 3, Name = "Calculus Quiz", Category = category, DifficultyLevel = difficulty, Description = "Quiz3" };
        Quiz quiz4 = new() { Id = 4, Name = "Statistics Quiz", Category = category, DifficultyLevel = difficulty, Description = "Quiz4" };

        return
            [
                new QuizAttempted { Id = 1, UserId = 1, Quiz = quiz1, TotalQue = 10, CorrectedQue = 8, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new QuizAttempted { Id = 2, UserId = 1, Quiz = quiz2, TotalQue = 10, CorrectedQue = 7, CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new QuizAttempted { Id = 3, UserId = 1, Quiz = quiz3, TotalQue = 0,  CorrectedQue = 0, CreatedDate = DateTime.UtcNow.AddDays(-3) },
                new QuizAttempted { Id = 4, UserId = 1, Quiz = quiz4, TotalQue = 10, CorrectedQue = 5, CreatedDate = DateTime.UtcNow }
            ];
    }

    [Fact]
    public async Task GetStatisticsData_ShouldReturnMappedResponse_WhenDataExists()
    {
        RawUserDashboardMetricsDTO rawMetrics = new()
        {
            UserName  = "User",
            QuizzesCompleted = 12,
            TotalXp = 2500,
            WinRate = 78.5,
            CurrentRank = 3
        };

        UserDashboardResponse expectedResponse = new()
        {
            UserName  = "User",
            QuizzesCompleted = 12,
            TotalXp = 2500,
            WinRate = 78.5,
            CurrentRank = 3
        };

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()))
            .ReturnsAsync(rawMetrics);

        _mapperMock
            .Setup(m => m.Map<UserDashboardResponse>(rawMetrics))
            .Returns(expectedResponse);

        UserDashboardResponse result = await _service.GetStatisticsData();

        Assert.NotNull(result);
        Assert.Equal(expectedResponse.UserName, result.UserName);
        Assert.Equal(expectedResponse.QuizzesCompleted, result.QuizzesCompleted);
        Assert.Equal(expectedResponse.TotalXp, result.TotalXp);
        Assert.Equal(expectedResponse.WinRate, result.WinRate);
        Assert.Equal(expectedResponse.CurrentRank, result.CurrentRank);

        _sqlQueryRepoMock.Verify(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDashboardResponse>(rawMetrics), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsData_ShouldReturnDefaultResponse_WhenRawMetricsIsNull()
    {
        UserDashboardResponse expectedResponse = new();

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()))
            .ReturnsAsync((RawUserDashboardMetricsDTO)null!);

        _mapperMock
            .Setup(m => m.Map<UserDashboardResponse>(null!))
            .Returns(expectedResponse);

        UserDashboardResponse result = await _service.GetStatisticsData();

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.UserName);
        Assert.Equal(0, result.QuizzesCompleted);
        Assert.Equal(0, result.TotalXp);
        Assert.Equal(0, result.WinRate);
        Assert.Equal(0, result.CurrentRank);

        _sqlQueryRepoMock.Verify(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDashboardResponse>(null!), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsData_ShouldThrowException_WhenSqlQueryFails()
    {
        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("SQL execution failed"));

        await Assert.ThrowsAsync<Exception>(() => _service.GetStatisticsData());

        _sqlQueryRepoMock.Verify(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDashboardResponse>(It.IsAny<RawUserDashboardMetricsDTO>()), Times.Never);
    }

    [Fact]
    public async Task GetStatisticsData_ShouldThrowException_WhenMapperFails()
    {
        RawUserDashboardMetricsDTO rawMetrics = new() { QuizzesCompleted = 5 };

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()))
            .ReturnsAsync(rawMetrics);

        _mapperMock
            .Setup(m => m.Map<UserDashboardResponse>(rawMetrics))
            .Throws(new AutoMapperMappingException("Mapping failed"));

        await Assert.ThrowsAsync<AutoMapperMappingException>(() => _service.GetStatisticsData());

        _sqlQueryRepoMock.Verify(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDashboardResponse>(rawMetrics), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsData_ShouldUseCorrectSqlQuery()
    {
        string executedSql = string.Empty;
        RawUserDashboardMetricsDTO rawMetrics = new();

        _sqlQueryRepoMock
            .Setup(repo => repo.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((sql, _) => executedSql = sql)
            .ReturnsAsync(new RawUserDashboardMetricsDTO());

        _mapperMock
            .Setup(m => m.Map<UserDashboardResponse>(rawMetrics))
            .Returns(new UserDashboardResponse());

        await _service.GetStatisticsData();

        Assert.Contains("get_user_dashboard_metrics", executedSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", executedSql);
    }

    [Fact]
    public async Task GetRecentQuizzes_ViewAll_ShouldReturnAllRecords()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        context.QuizAttempteds.AddRange(GetSampleData());
        context.SaveChanges();

        _quizAttemptedRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizAttempted, object>>[]>()))
            .Returns(context.QuizAttempteds.Include(q => q.Quiz));

        List<RecentQuizResponse> result = await _service.GetRecentQuizzes(true);

        Assert.Equal(4, result.Count);
        Assert.Equal("Statistics Quiz", result.First().QuizName);
    }

    [Fact]
    public async Task GetRecentQuizzes_ViewAllFalse_ShouldReturnOnlyTop3()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new QuizVerseDbContext(options);
        context.QuizAttempteds.AddRange(GetSampleData());
        await context.SaveChangesAsync();

        _quizAttemptedRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizAttempted, object>>[]>()))
            .Returns(context.QuizAttempteds);

        var result = await _service.GetRecentQuizzes(false);

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, r => r.QuizName == "Calculus Quiz");
    }

    [Fact]
    public async Task GetRecentQuizzes_NoData_ShouldReturnEmptyList()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        _quizAttemptedRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizAttempted, object>>[]>()))
            .Returns(context.QuizAttempteds);

        List<RecentQuizResponse> result = await _service.GetRecentQuizzes(true);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentQuizzes_ShouldCalculateScoreCorrectly()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        context.QuizAttempteds.AddRange(GetSampleData());
        await context.SaveChangesAsync();

        _quizAttemptedRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizAttempted, object>>[]>()))
            .Returns(context.QuizAttempteds);

        List<RecentQuizResponse> result = await _service.GetRecentQuizzes(true);

        RecentQuizResponse algebraQuiz = result.Single(r => r.QuizName == "Algebra Quiz");
        Assert.Equal(80.0, algebraQuiz.Score);

        RecentQuizResponse geometryQuiz = result.Single(r => r.QuizName == "Geometry Quiz");
        Assert.Equal(70.0, geometryQuiz.Score);

        RecentQuizResponse calculusQuiz = result.Single(r => r.QuizName == "Calculus Quiz");
        Assert.Equal(0, calculusQuiz.Score);
    }

    [Fact]
    public async Task GetRecentQuizzes_ShouldMapPropertiesCorrectly()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        context.QuizAttempteds.AddRange(GetSampleData());
        await context.SaveChangesAsync();

        _quizAttemptedRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizAttempted, object>>[]>()))
            .Returns(context.QuizAttempteds);

        List<RecentQuizResponse> result = await _service.GetRecentQuizzes(true);

        RecentQuizResponse first = result.First();
        Assert.False(string.IsNullOrWhiteSpace(first.QuizName));
        Assert.False(string.IsNullOrWhiteSpace(first.CategoryName));
        Assert.False(string.IsNullOrWhiteSpace(first.DifficultyLevel));
        Assert.NotEqual(default, first.AttemptedOn);
    }

    [Fact]
    public async Task GetFeaturedQuizzes_InvalidBatchNumber_ThrowsAppException()
    {
        int invalidBatch = Constants.MAX_BATCH + 1;

        AppException ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.GetFeaturedQuizzes(invalidBatch));

        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);

        string expectedMessage = string.Format(Constants.INVALID_BATCH_NUMBER, Constants.MIN_BATCH, Constants.MAX_BATCH);
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task GetFeaturedQuizzes_NoFeaturedQuizzes_ReturnsEmptyList()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        context.Quizzes.AddRange(new List<Quiz>
        {
            new() { Id = 1, Name = "Quiz 1", Description = "Description1", IsFeatured = false, Status = (int)QuizStatus.Active, IsDeleted = false },
            new() { Id = 2, Name = "Quiz 2", Description = "Description1", IsFeatured = false, Status = (int)QuizStatus.Active, IsDeleted = false }
        });

        await context.SaveChangesAsync();

        _quizRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<Quiz, object>>[]>()))
            .Returns(context.Quizzes);

        FeaturedQuizListDTO result = await _service.GetFeaturedQuizzes(1);

        Assert.NotNull(result);
        Assert.Empty(result.Quizzes);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetFeaturedQuizzes_SomeFeaturedQuizzes_ReturnsCorrectList()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        QuizCategory category = new() { Id = 1, CategoryName = "Science", Description = "Category1" };
        QuizDifficulty difficulty = new() { Id = 1, Name = "Easy", Description = "Difficulty1" };

        context.Quizzes.AddRange(new List<Quiz>
        {
            new() {
                Id = 1,
                Name = "Quiz 1",
                Description = "Description 1",
                IsFeatured = true,
                Status = (int)QuizStatus.Active,
                IsDeleted = false,
                Rating = 4.5m,
                Category = category,
                DifficultyLevel = difficulty,
                CreatedDate = DateTime.UtcNow,
                QuizAttempteds = [new QuizAttempted(), new QuizAttempted()]
            },
            new() {
                Id = 2,
                Name = "Quiz 2",
                Description = "Description 2",
                IsFeatured = true,
                Status = (int)QuizStatus.Active,
                IsDeleted = false,
                Rating = 4.8m,
                Category = category,
                DifficultyLevel = difficulty,
                CreatedDate = DateTime.UtcNow,
                QuizAttempteds = [new QuizAttempted()]
            }
        });

        await context.SaveChangesAsync();

        _quizRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<Quiz, object>>[]>()))
            .Returns(context.Quizzes);

        FeaturedQuizListDTO result = await _service.GetFeaturedQuizzes(1);

        Assert.NotNull(result);
        Assert.Equal(2, result.Quizzes.Count);

        Assert.Equal(1, result.Quizzes[0].QuizId);
        Assert.Equal(2, result.Quizzes[1].QuizId);
    }

    [Fact]
    public async Task GetFeaturedQuizzes_SkipExceedsTotal_ReturnsEmptyWithHasMoreFalse()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        QuizCategory category = new() { Id = 1, CategoryName = "Science", Description = "Category1" };
        QuizDifficulty difficulty = new() { Id = 1, Name = "Easy", Description = "Difficulty1" };

        context.Quizzes.Add(new Quiz
        {
            Id = 1,
            Name = "Quiz 1",
            Description = "Sample description",
            IsFeatured = true,
            Status = (int)QuizStatus.Active,
            IsDeleted = false,
            Rating = 4.5m,
            Category = category,
            DifficultyLevel = difficulty,
            QuizAttempteds = [new QuizAttempted()]
        });

        await context.SaveChangesAsync();

        _quizRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<Quiz, object>>[]>()))
            .Returns(context.Quizzes);

        int batchNumber = 2;

        FeaturedQuizListDTO result = await _service.GetFeaturedQuizzes(batchNumber);

        Assert.NotNull(result);
        Assert.Empty(result.Quizzes);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetBattleRequests_NoRequests_ReturnsEmptyList()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new QuizVerseDbContext(options);

        _battleRequestRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<BattleRequest, object>>[]>()))
            .Returns(context.BattleRequests);

        List<BattleRequestDTO> result = await _service.GetBattleRequests();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBattleRequests_WithRequests_ReturnsMappedDTOs()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new(options);

        QuizCategory category = new() { Id = 1, CategoryName = "Science", Description = "Category1" };
        QuizDifficulty difficulty = new() { Id = 1, Name = "Easy", Description = "Difficulty1" };
        Quiz quiz = new() { Id = 1, Name = "Quiz 1", Description = "Desc", Category = category, DifficultyLevel = difficulty };
        BattleList battle = new() { Id = 1, Quiz = quiz, BattleTimeLimited = false };
        User sender = new() { Id = 1, UserName = "Sender1", IsDeleted = false, ProfilePic = "pic.png", Email = "sender1@example.com", FullName = "Sender One", Password = "hashedpassword" };
        int receiverId = 1;

        context.BattleRequests.Add(new BattleRequest
        {
            Id = 1,
            Sender = sender,
            ReceiverId = receiverId,
            Battle = battle,
            SendingDate = DateTime.UtcNow.AddMinutes(-5),
            Status = (int)BattleRequestStatus.Pending,
            IsDeleted = false
        });

        await context.SaveChangesAsync();

        _battleRequestRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<BattleRequest, object>>[]>()))
            .Returns(context.BattleRequests);

        List<BattleRequestDTO> result = await _service.GetBattleRequests();

        Assert.NotNull(result);
        Assert.Single(result);

        BattleRequestDTO dto = result.First();
        Assert.Equal(1, dto.RequestId);
        Assert.Equal("Sender1", dto.SenderUserName);
        Assert.Equal("pic.png", dto.SenderProfilePic);
        Assert.Equal("Quiz 1", dto.BattleName);
        Assert.Equal("Science", dto.BattleCategory);
        Assert.Equal("Easy", dto.BattleDifficulty);
        Assert.NotEqual(default, dto.SendingDate);
        Assert.False(string.IsNullOrWhiteSpace(dto.TimeAgo));
    }

    [Fact]
    public async Task GetBattleRequests_ExcludesDeletedOrNonPending()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new QuizVerseDbContext(options);

        QuizCategory category = new() { Id = 1, CategoryName = "Science", Description = "Category1" };
        QuizDifficulty difficulty = new() { Id = 1, Name = "Easy", Description = "Difficulty1" };
        Quiz quiz = new() { Id = 1, Name = "Quiz 1", Description = "Desc", Category = category, DifficultyLevel = difficulty };
        BattleList battle = new() { Id = 1, Quiz = quiz, BattleTimeLimited = false };
        User sender = new() { Id = 1, UserName = "Sender1", IsDeleted = true, ProfilePic = "pic.png", Email = "sender1@example.com", FullName = "Sender One", Password = "hashedpassword" };
        int receiverId = 1;

        context.BattleRequests.AddRange(new List<BattleRequest>
        {
            new() {
                Id = 1,
                Sender = sender,
                ReceiverId = receiverId,
                Battle = battle,
                SendingDate = DateTime.UtcNow,
                Status = (int)BattleRequestStatus.Pending,
                IsDeleted = true
            },
            new() {
                Id = 2,
                Sender = new User
                {
                    Id = 2,
                    UserName = "Sender2",
                    Email = "sender2@example.com",
                    FullName = "Sender Two",
                    Password = "hashedpassword",
                    ProfilePic = "pic2.png",
                    IsDeleted = false
                },
                ReceiverId = receiverId,
                Battle = battle,
                SendingDate = DateTime.UtcNow,
                Status = (int)BattleRequestStatus.Accepted,
                IsDeleted = false
            }
        });

        await context.SaveChangesAsync();

        _battleRequestRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<BattleRequest, object>>[]>()))
            .Returns(context.BattleRequests);

        List<BattleRequestDTO> result = await _service.GetBattleRequests();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBattleRequests_RespectsTimeLimitedBattles()
    {
        DbContextOptions<QuizVerseDbContext> options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using QuizVerseDbContext context = new QuizVerseDbContext(options);

        QuizCategory category = new() { Id = 1, CategoryName = "Science", Description = "Category1" };
        QuizDifficulty difficulty = new() { Id = 1, Name = "Easy", Description = "Difficulty1" };
        Quiz quiz = new() { Id = 1, Name = "Quiz 1", Description = "Desc", Category = category, DifficultyLevel = difficulty };

        BattleList battleExpired = new()
        {
            Id = 1,
            Quiz = quiz,
            BattleTimeLimited = true,
            StartDate = DateTime.UtcNow.AddHours(-2),
            EndDate = DateTime.UtcNow.AddHours(-1)
        };

        BattleList battleRunning = new()
        {
            Id = 2,
            Quiz = quiz,
            BattleTimeLimited = true,
            StartDate = DateTime.UtcNow.AddMinutes(-5),
            EndDate = DateTime.UtcNow.AddMinutes(5)
        };

        int receiverId = 1;

        context.BattleRequests.AddRange(new List<BattleRequest>
        {
            new() {
                Id = 1,
                Sender = new User
                {
                    Id = 1,
                    UserName = "Sender1",
                    Email = "sender1@example.com",
                    FullName = "Sender One",
                    Password = "hashedpassword",
                    ProfilePic = "pic1.png",
                    IsDeleted = false
                },
                ReceiverId = receiverId,
                Battle = battleExpired,
                SendingDate = DateTime.UtcNow,
                Status = (int)BattleRequestStatus.Pending,
                IsDeleted = false
            },
            new() {
                Id = 2,
                Sender = new User
                {
                    Id = 2,
                    UserName = "Sender2",
                    Email = "sender2@example.com",
                    FullName = "Sender Two",
                    Password = "hashedpassword",
                    ProfilePic = "pic2.png",
                    IsDeleted = false
                },
                ReceiverId = receiverId,
                Battle = battleRunning,
                SendingDate = DateTime.UtcNow,
                Status = (int)BattleRequestStatus.Pending,
                IsDeleted = false
            }
        });

        await context.SaveChangesAsync();

        _battleRequestRepoMock
            .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<BattleRequest, object>>[]>()))
            .Returns(context.BattleRequests);

        List<BattleRequestDTO> result = await _service.GetBattleRequests();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(2, result.First().RequestId);
    }

    [Fact]
    public async Task UpdateBattleRequestStatus_RequestExists_UpdatesStatusAndReturnsTrue()
    {
        int requestId = 1;
        BattleRequestActionDTO dto = new()
        {
            RequestId = requestId,
            Status = (int)BattleRequestStatus.Accepted
        };

        BattleRequest existingRequest = new()
        {
            Id = requestId,
            Status = (int)BattleRequestStatus.Pending,
            IsDeleted = false
        };

        _battleRequestRepoMock
        .Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<BattleRequest, bool>>>(),
            It.IsAny<Func<IQueryable<BattleRequest>, IQueryable<BattleRequest>>?>()
        ))
        .ReturnsAsync(existingRequest);

        _battleRequestRepoMock
            .Setup(r => r.UpdateAsync(existingRequest))
            .Returns(Task.CompletedTask)
            .Verifiable();

        bool result = await _service.UpdateBattleRequestStatus(dto);

        Assert.True(result);
        Assert.Equal(dto.Status, existingRequest.Status);
        Assert.True(existingRequest.ModifiedDate <= DateTime.UtcNow);
        _battleRequestRepoMock.Verify(r => r.UpdateAsync(existingRequest), Times.Once);
    }

    [Fact]
    public async Task UpdateBattleRequestStatus_RequestDoesNotExist_ThrowsAppException()
    {
        int requestId = 1;
        BattleRequestActionDTO dto = new()
        {
            RequestId = requestId,
            Status = (int)BattleRequestStatus.Accepted
        };

        _battleRequestRepoMock
        .Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<BattleRequest, bool>>>(),
            It.IsAny<Func<IQueryable<BattleRequest>, IQueryable<BattleRequest>>?>()
        ))
        .ReturnsAsync((BattleRequest?)null);

        AppException exception = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateBattleRequestStatus(dto));

        Assert.Contains(requestId.ToString(), exception.Message);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetRankProgressAsync_ReturnsMappedDTO_WhenSqlQueryReturnsData()
    {
        RawRankProgressDTO rawDto = new()
        {
            CurrentRank = "Silver",
            NextRank = "Gold",
            XpNeeded = 500,
            ProgressPercent = 60.5m
        };

        RankProgressDTO expectedDto = new()
        {
            CurrentRank = "Silver",
            NextRank = "Gold",
            XpNeeded = 500,
            ProgressPercent = 60.5m
        };

        _sqlQueryRepoMock
            .Setup(r => r.SqlQuerySingleAsync<RawRankProgressDTO>(
                It.IsAny<string>()
            ))
            .ReturnsAsync(rawDto);

        _mapperMock
            .Setup(m => m.Map<RankProgressDTO>(rawDto))
            .Returns(expectedDto);

        RankProgressDTO result = await _service.GetRankProgressAsync();

        Assert.NotNull(result);
        Assert.Equal(expectedDto.CurrentRank, result.CurrentRank);
        Assert.Equal(expectedDto.NextRank, result.NextRank);
        Assert.Equal(expectedDto.XpNeeded, result.XpNeeded);
        Assert.Equal(expectedDto.ProgressPercent, result.ProgressPercent);

        _sqlQueryRepoMock.Verify(r => r.SqlQuerySingleAsync<RawRankProgressDTO>(
            It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<RankProgressDTO>(rawDto), Times.Once);
    }

    [Fact]
    public async Task GetRankProgressAsync_ReturnsDummy_WhenSqlQueryWouldReturnNull()
    {
        RawRankProgressDTO dummyRawDto = new()
        {
            CurrentRank = string.Empty,
            NextRank = string.Empty,
            XpNeeded = 0,
            ProgressPercent = 0
        };

        RankProgressDTO dummyMappedDto = new()
        {
            CurrentRank = string.Empty,
            NextRank = string.Empty,
            XpNeeded = 0,
            ProgressPercent = 0
        };

        _sqlQueryRepoMock
            .Setup(r => r.SqlQuerySingleAsync<RawRankProgressDTO>(It.IsAny<string>()))
            .ReturnsAsync(dummyRawDto);

        _mapperMock
            .Setup(m => m.Map<RankProgressDTO>(dummyRawDto))
            .Returns(dummyMappedDto);

        RankProgressDTO result = await _service.GetRankProgressAsync();

        Assert.NotNull(result);
        Assert.Equal(dummyMappedDto.CurrentRank, result.CurrentRank);
        Assert.Equal(dummyMappedDto.NextRank, result.NextRank);
        Assert.Equal(dummyMappedDto.XpNeeded, result.XpNeeded);
        Assert.Equal(dummyMappedDto.ProgressPercent, result.ProgressPercent);

        _sqlQueryRepoMock.Verify(r => r.SqlQuerySingleAsync<RawRankProgressDTO>(It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<RankProgressDTO>(dummyRawDto), Times.Once);
    }
}
