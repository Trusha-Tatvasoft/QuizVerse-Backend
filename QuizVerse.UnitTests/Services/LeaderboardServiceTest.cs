using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Expressions;
using System.Security.Claims;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class LeaderboardServiceTests
    {
        private readonly Mock<IGenericRepository<UserPerformanceDetail>> _leaderboardRepoMock;
        private readonly Mock<IGenericRepository<QuizCategory>> _quizCategoryRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly LeaderboardService _service;
        private readonly Mock<IMemoryCacheService> _cacheServiceMock;
        private readonly Mock<IGenericRepository<QuizAttempted>> _quizAttemptedRepoMock;
        private readonly Mock<IGenericRepository<BattleStatus>> _battleStatusRepoMock;

        public LeaderboardServiceTests()
        {
            _leaderboardRepoMock = new Mock<IGenericRepository<UserPerformanceDetail>>();
            _quizCategoryRepoMock = new Mock<IGenericRepository<QuizCategory>>();
            _mapperMock = new Mock<IMapper>();
            _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _cacheServiceMock = new Mock<IMemoryCacheService>();
            _quizAttemptedRepoMock = new Mock<IGenericRepository<QuizAttempted>>();
            _battleStatusRepoMock = new Mock<IGenericRepository<BattleStatus>>();

            // Setup HTTP context with user ID claim
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.UserData, "1") }, "mock"));
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new LeaderboardService(
                _leaderboardRepoMock.Object,
                _quizCategoryRepoMock.Object, // Added missing dependency
                _httpContextAccessorMock.Object,
                _mapperMock.Object,
                _sqlQueryRepoMock.Object,
                _quizAttemptedRepoMock.Object,
                _battleStatusRepoMock.Object,
                _cacheServiceMock.Object
            );
        }

        [Fact]
        public async Task GetUserLeaderboardStats_ReturnsMappedDto_WhenUserExists()
        {
            // Arrange
            var userPerformance = new UserPerformanceDetail
            {
                UserId = 1,
                TotalXp = 500,
                CurrentLevel = 5,
                NewGlobalRank = 10
            };

            var mappedDto = new UserPerformanceResponseDto
            {
                TotalXp = 500,
                CurrentLevel = 5,
                GlobalRank = 10
            };

            _leaderboardRepoMock
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<UserPerformanceDetail, bool>>>(),
                    It.IsAny<Func<IQueryable<UserPerformanceDetail>, IQueryable<UserPerformanceDetail>>>()))
                .ReturnsAsync(userPerformance);

            _mapperMock
                .Setup(m => m.Map<UserPerformanceResponseDto>(It.IsAny<UserPerformanceDetail>()))
                .Returns(mappedDto);

            // Act
            var result = await _service.GetUserLeaderboardStats();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mappedDto.GlobalRank, result.GlobalRank);
            Assert.Equal(mappedDto.TotalXp, result.TotalXp);
            Assert.Equal(mappedDto.CurrentLevel, result.CurrentLevel);
        }

        [Fact]
        public async Task GetUserLeaderboardStats_ReturnsEmptyDto_WhenUserNotFound()
        {
            // Arrange
            _leaderboardRepoMock
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<UserPerformanceDetail, bool>>>(),
                    It.IsAny<Func<IQueryable<UserPerformanceDetail>, IQueryable<UserPerformanceDetail>>>()))
                .ReturnsAsync((UserPerformanceDetail?)null);

            _mapperMock
                .Setup(m => m.Map<UserPerformanceResponseDto>(It.IsAny<UserPerformanceDetail>()))
                .Returns(new UserPerformanceResponseDto());

            // Act
            var result = await _service.GetUserLeaderboardStats();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.GlobalRank);
            Assert.Equal(0, result.TotalXp);
            Assert.Equal(0, result.CurrentLevel);
        }

        [Fact]
        public async Task GetLeaderboardGlobalRanking_ReturnsMappedLeaderboard()
        {
            // Arrange
            var rawLeaderboard = new List<RawLeaderboardGlobalRankingDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, TotalXp = 1000, CurrentLevel = 10, CurrentStreak = 5, Trend = 2, IsLoggedInUser = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, TotalXp = 900, CurrentLevel = 9, CurrentStreak = 3, Trend = 3, IsLoggedInUser = false }
            };

            var mappedLeaderboard = new List<LeaderboardGlobalRankingResponseDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, TotalXp = 1000, CurrentLevel = 10, CurrentStreak = 5, Trend = 2, IsLoggedInUser = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, TotalXp = 900, CurrentLevel = 9, CurrentStreak = 3, Trend = 3, IsLoggedInUser = false }
            };

            _sqlQueryRepoMock
                .Setup(x => x.SqlQueryListAsync<RawLeaderboardGlobalRankingDto>(
                    It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(rawLeaderboard);

            _mapperMock
                .Setup(m => m.Map<List<LeaderboardGlobalRankingResponseDto>>(It.IsAny<List<RawLeaderboardGlobalRankingDto>>()))
                .Returns(mappedLeaderboard);

            // Act
            var result = await _service.GetLeaderboardGlobalRanking();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("user1", result[0].UserName);
            Assert.True(result[0].IsLoggedInUser);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<RawLeaderboardGlobalRankingDto>(
                It.IsAny<string>(), It.Is<NpgsqlParameter[]>(p =>
                    p.Length == 1 && Convert.ToInt32(p[0].Value) == 1)), Times.Once);

            _mapperMock.Verify(m => m.Map<List<LeaderboardGlobalRankingResponseDto>>(It.IsAny<List<RawLeaderboardGlobalRankingDto>>()), Times.Once);
        }

        [Fact]
        public async Task GetWeeklyLeaderboardRanking_ReturnsWeeklyLeaderboard()
        {
            // Arrange
            var weeklyLeaderboard = new List<WeeklyLeaderBoardResponseDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, TotalXp = 500, TotalQuizzesPlayed = 10, TotalBattlesPlayed = 5, IsLoggedInUser = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, TotalXp = 400, TotalQuizzesPlayed = 8, TotalBattlesPlayed = 3, IsLoggedInUser = false }
            };

            _sqlQueryRepoMock
                .Setup(x => x.SqlQueryListAsync<WeeklyLeaderBoardResponseDto>(
                    It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(weeklyLeaderboard);

            // Act
            var result = await _service.GetWeeklyLeaderboardRanking();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("user1", result[0].UserName);
            Assert.True(result[0].IsLoggedInUser);
            Assert.Equal(500, result[0].TotalXp);
            Assert.Equal(10, result[0].TotalQuizzesPlayed);
            Assert.Equal(5, result[0].TotalBattlesPlayed);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<WeeklyLeaderBoardResponseDto>(
                It.IsAny<string>(), It.Is<NpgsqlParameter[]>(p =>
                    p.Length == 1 && Convert.ToInt32(p[0].Value) == 1)), Times.Once);
        }

        [Fact]
        public async Task GetQuizCategoryWiseLeaderboardRanking_ReturnsCategoryLeaderboard_WhenCategoryExists()
        {
            // Arrange
            var categoryId = 1;
            var quizCategory = new QuizCategory { Id = categoryId, CategoryName = "General Knowledge" };
            var categoryLeaderboard = new List<CategoryWiseLeaderBoardResponseDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, AverageScore = 95.5m, TotalQuizzesPlayed = 10, TotalBattlesPlayed = 5, IsLoggedInUser = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, AverageScore = 90.0m, TotalQuizzesPlayed = 8, TotalBattlesPlayed = 3, IsLoggedInUser = false }
            };

            _quizCategoryRepoMock
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<QuizCategory, bool>>>(),
                    It.IsAny<Func<IQueryable<QuizCategory>, IQueryable<QuizCategory>>>()))
                .ReturnsAsync(quizCategory);

            _sqlQueryRepoMock
                .Setup(x => x.SqlQueryListAsync<CategoryWiseLeaderBoardResponseDto>(
                    It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(categoryLeaderboard);

            // Act
            var result = await _service.GetQuizCategoryWiseLeaderboardRanking(categoryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("user1", result[0].UserName);
            Assert.True(result[0].IsLoggedInUser);
            Assert.Equal(95.5m, result[0].AverageScore);
            Assert.Equal(10, result[0].TotalQuizzesPlayed);
            Assert.Equal(5, result[0].TotalBattlesPlayed);

            _quizCategoryRepoMock.Verify(x => x.GetAsync(
                It.Is<Expression<Func<QuizCategory, bool>>>(expr => expr.Compile()(new QuizCategory { Id = categoryId })),
                It.IsAny<Func<IQueryable<QuizCategory>, IQueryable<QuizCategory>>>()), Times.Once);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<CategoryWiseLeaderBoardResponseDto>(
                It.IsAny<string>(), It.Is<NpgsqlParameter[]>(p =>
                    p.Length == 2 && Convert.ToInt32(p[0].Value) == 1 && Convert.ToInt32(p[1].Value) == categoryId)), Times.Once);
        }

        [Fact]
        public async Task GetQuizCategoryWiseLeaderboardRanking_ThrowsKeyNotFoundException_WhenCategoryNotFound()
        {
            // Arrange
            var categoryId = 999;
            _quizCategoryRepoMock
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<QuizCategory, bool>>>(),
                    It.IsAny<Func<IQueryable<QuizCategory>, IQueryable<QuizCategory>>>()))
                .ReturnsAsync((QuizCategory?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppException>(() =>
                _service.GetQuizCategoryWiseLeaderboardRanking(categoryId));
            Assert.Equal(Constants.QUIZ_CATEGORY_NOT_FOUND_MESSAGE, exception.Message);

            _quizCategoryRepoMock.Verify(x => x.GetAsync(
                It.Is<Expression<Func<QuizCategory, bool>>>(expr => expr.Compile()(new QuizCategory { Id = categoryId })),
                It.IsAny<Func<IQueryable<QuizCategory>, IQueryable<QuizCategory>>>()), Times.Once);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<CategoryWiseLeaderBoardResponseDto>(
                It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Never);
        }

        [Fact]
        public async Task GetMonthlyChampions_ReturnsMonthlyChampions_WhenMonthAndYearAreValid()
        {
            // Arrange
            var month = 6;
            var year = 2024;
            var monthlyChampions = new List<MonthlyChampionsResponseDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, TotalXp = 1000, AverageScore = 95.5m, TotalQuizzesPlayed = 10, TotalBattlesPlayed = 5, IsLoggedInUser = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, TotalXp = 900, AverageScore = 90.0m, TotalQuizzesPlayed = 8, TotalBattlesPlayed = 3, IsLoggedInUser = false }
            };

            _sqlQueryRepoMock
                .Setup(x => x.SqlQueryListAsync<MonthlyChampionsResponseDto>(
                    It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(monthlyChampions);

            // Act
            var result = await _service.GetMonthlyChampions(month, year);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("user1", result[0].UserName);
            Assert.True(result[0].IsLoggedInUser);
            Assert.Equal(1000, result[0].TotalXp);
            Assert.Equal(95.5m, result[0].AverageScore);
            Assert.Equal(10, result[0].TotalQuizzesPlayed);
            Assert.Equal(5, result[0].TotalBattlesPlayed);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<MonthlyChampionsResponseDto>(
                It.IsAny<string>(), It.Is<NpgsqlParameter[]>(p =>
                    p.Length == 3 && Convert.ToInt32(p[0].Value) == 1 && Convert.ToInt32(p[1].Value) == month && Convert.ToInt32(p[2].Value) == year)), Times.Once);
        }

        [Fact]
        public async Task GetMonthlyChampions_ThrowsAppException_WhenMonthIsInvalid()
        {
            // Arrange
            var month = 13;
            var year = 2024;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppException>(() =>
                _service.GetMonthlyChampions(month, year));
            Assert.Equal(Constants.INVALID_MONTH_MESSAGE, exception.Message);
            Assert.Equal(400, exception.StatusCode);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<MonthlyChampionsResponseDto>(
                It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Never);
        }

        [Fact]
        public async Task GetMonthlyChampions_ThrowsAppException_WhenYearIsBefore2023()
        {
            // Arrange
            var month = 6;
            var year = 2022;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppException>(() =>
                _service.GetMonthlyChampions(month, year));
            Assert.Equal(Constants.INVALID_YEAR_MESSAGE, exception.Message);
            Assert.Equal(400, exception.StatusCode);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<MonthlyChampionsResponseDto>(
                It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Never);
        }

        [Fact]
        public async Task GetMonthlyChampions_ThrowsAppException_WhenYearIsFuture()
        {
            // Arrange
            var month = 6;
            var year = DateTime.Now.Year + 1; // Future year relative to September 5, 2025
 
            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppException>(() =>
                _service.GetMonthlyChampions(month, year));
            Assert.Equal(Constants.INVALID_YEAR_MESSAGE, exception.Message);
            Assert.Equal(400, exception.StatusCode);
 
            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<MonthlyChampionsResponseDto>(
                It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Never);
        }
 
        [Fact]
        public async Task GetMonthlyChampions_ThrowsAppException_WhenMonthIsFutureInCurrentYear()
        {
            // Arrange
            var month = DateTime.Now.Month + 1; // Future month relative to September 5, 2025
            var year = DateTime.Now.Year;
 
            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppException>(() =>
                _service.GetMonthlyChampions(month, year));
            Assert.Equal(Constants.INVALID_MONTH_YEAR_COMBINATION_MESSAGE, exception.Message);
            Assert.Equal(400, exception.StatusCode);
 
            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<MonthlyChampionsResponseDto>(
                It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Never);
        }

        [Fact]
        public void GetAvailableYears_ReturnsCachedValue_WhenCacheExists()
        {
            List<CommonListDropDownDto> cachedYears = [new() { Id = 2025, Name = "2025" }];
            _cacheServiceMock.Setup(c => c.GetOrSet("AvailableYears", It.IsAny<Func<List<CommonListDropDownDto>>>()))
                .Returns(cachedYears);

            List<CommonListDropDownDto> result = _service.GetAvailableYears();

            Assert.Single(result);
            Assert.Equal(2025, result[0].Id);
            _cacheServiceMock.Verify(c => c.GetOrSet("AvailableYears", It.IsAny<Func<List<CommonListDropDownDto>>>()), Times.Once);
        }

        [Fact]
        public void ClearAvailableYearsCache_CallsCacheClear()
        {
            _service.ClearAvailableYearsCache();

            _cacheServiceMock.Verify(c => c.Clear("AvailableYears"), Times.Once);
        }

        [Fact]
        public void GetAvailableMonthsByYear_ReturnsCachedValue_WhenCacheExists()
        {
            int year = 2025;
            List<CommonListDropDownDto> cachedMonths = [
                new() { Id = 1, Name = "January" },
                new() { Id = 2, Name = "February" }
            ];

            _cacheServiceMock.Setup(c => c.GetOrSet($"AvailableMonths_{year}", It.IsAny<Func<List<CommonListDropDownDto>>>()))
                .Returns(cachedMonths);

            List<CommonListDropDownDto> result = _service.GetAvailableMonthsByYear(year);

            Assert.Equal(2, result.Count);
            Assert.Equal("January", result[0].Name);
            Assert.Equal("February", result[1].Name);
            _cacheServiceMock.Verify(c => c.GetOrSet($"AvailableMonths_{year}", It.IsAny<Func<List<CommonListDropDownDto>>>()), Times.Once);
        }

        [Fact]
        public void ClearAvailableMonthsCache_CallsCacheClearWithYear()
        {
            int year = 2025;

            _service.ClearAvailableMonthsCache(year);

            _cacheServiceMock.Verify(c => c.Clear($"AvailableMonths_{year}"), Times.Once);
        }

        [Fact]
        public void GetAvailableYears_ComputesDistinctYears_WhenCacheEmpty()
        {
            IQueryable<QuizAttempted> quizData = new List<QuizAttempted>
            {
                new() { CreatedDate = new DateTime(2023, 1, 1) },
                new() { CreatedDate = new DateTime(2024, 1, 1) }
            }.AsQueryable();

            IQueryable<BattleStatus> battleData = new List<BattleStatus>
            {
                new() { CreatedDate = new DateTime(2024, 1, 1) },
                new() { CreatedDate = new DateTime(2025, 1, 1) }
            }.AsQueryable();

            _quizAttemptedRepoMock.Setup(r => r.GetQueryableInclude()).Returns(quizData);
            _battleStatusRepoMock.Setup(r => r.GetQueryableInclude()).Returns(battleData);
            _cacheServiceMock.Setup(c => c.GetOrSet("AvailableYears", It.IsAny<Func<List<CommonListDropDownDto>>>()))
                .Returns((string key, Func<List<CommonListDropDownDto>> factory) => factory());

            List<CommonListDropDownDto> result = _service.GetAvailableYears();

            Assert.Equal(3, result.Count);
            Assert.Equal(2025, result[0].Id);
            Assert.Equal(2024, result[1].Id);
            Assert.Equal(2023, result[2].Id);
        }

        [Fact]
        public void GetAvailableMonthsByYear_ComputesDistinctMonths_WhenCacheEmpty()
        {
            int year = 2024;
            IQueryable<QuizAttempted> quizData = new List<QuizAttempted>
            {
                new() { CreatedDate = new DateTime(year, 3, 1) },
                new() { CreatedDate = new DateTime(year, 1, 1) }
            }.AsQueryable();

            IQueryable<BattleStatus> battleData = new List<BattleStatus>
            {
                new() { CreatedDate = new DateTime(year, 2, 1) },
                new() { CreatedDate = new DateTime(year, 3, 1) }
            }.AsQueryable();

            _quizAttemptedRepoMock.Setup(r => r.GetQueryableInclude()).Returns(quizData);
            _battleStatusRepoMock.Setup(r => r.GetQueryableInclude()).Returns(battleData);

            _cacheServiceMock.Setup(c => c.GetOrSet($"AvailableMonths_{year}", It.IsAny<Func<List<CommonListDropDownDto>>>()))
                .Returns((string key, Func<List<CommonListDropDownDto>> factory) => factory());

            List<CommonListDropDownDto> result = _service.GetAvailableMonthsByYear(year);

            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
            Assert.Equal(3, result[2].Id);
            Assert.Equal("January", result[0].Name);
            Assert.Equal("February", result[1].Name);
            Assert.Equal("March", result[2].Name);
        }
    }
}