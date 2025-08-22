using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class LeaderboardServiceTests
    {
        private readonly Mock<IGenericRepository<UserPerformanceDetail>> _leaderboardRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly LeaderboardService _service;

        public LeaderboardServiceTests()
        {
            _leaderboardRepoMock = new Mock<IGenericRepository<UserPerformanceDetail>>();
            _mapperMock = new Mock<IMapper>();
            _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Corrected claim setup
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, "1")], "mock"));
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new LeaderboardService(
                _leaderboardRepoMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object,
                _sqlQueryRepoMock.Object
            );
        }

        [Fact]
        public async Task GetUserLeaderboardStats_ReturnsMappedDto_WhenUserExists()
        {
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

            var result = await _service.GetUserLeaderboardStats();

            Assert.NotNull(result);
            Assert.Equal(mappedDto.GlobalRank, result.GlobalRank);
            Assert.Equal(mappedDto.TotalXp, result.TotalXp);
            Assert.Equal(mappedDto.CurrentLevel, result.CurrentLevel);
        }

        [Fact]
        public async Task GetUserLeaderboardStats_ReturnsEmptyDto_WhenUserNotFound()
        {
            _leaderboardRepoMock
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<UserPerformanceDetail, bool>>>(),
                    It.IsAny<Func<IQueryable<UserPerformanceDetail>, IQueryable<UserPerformanceDetail>>>()))
                .ReturnsAsync((UserPerformanceDetail?)null);

            _mapperMock
                .Setup(m => m.Map<UserPerformanceResponseDto>(It.IsAny<UserPerformanceDetail>()))
                .Returns(new UserPerformanceResponseDto());

            var result = await _service.GetUserLeaderboardStats();

            Assert.NotNull(result);
            Assert.Equal(0, result.GlobalRank);
            Assert.Equal(0, result.TotalXp);
            Assert.Equal(0, result.CurrentLevel);
        }

        [Fact]
        public async Task GetLeaderboardGlobalRanking_ReturnsMappedLeaderboard()
        {
            // Arrange
            DefaultHttpContext httpContext = new ()
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(ClaimTypes.UserData, "1")], "mock")
                )
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            var rawLeaderboard = new List<RawLeaderboardGlobalRankingDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, TotalXp = 1000, CurrentLevel = 10, CurrentStreak = 5, NewGlobalRank = 1, Trend = 2, AmI = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, TotalXp = 900, CurrentLevel = 9, CurrentStreak = 3, NewGlobalRank = 2, Trend = 3, AmI = false }
            };

            var mappedLeaderboard = new List<LeaderboardGlobalRankingResponseDto>
            {
                new() { Rank = 1, UserId = 1, UserName = "user1", FullName = "User One", ProfilePic = null, TotalXp = 1000, CurrentLevel = 10, CurrentStreak = 5, NewGlobalRank = 1, Trend = 2, AmI = true },
                new() { Rank = 2, UserId = 2, UserName = "user2", FullName = "User Two", ProfilePic = null, TotalXp = 900, CurrentLevel = 9, CurrentStreak = 3, NewGlobalRank = 2, Trend = 3, AmI = false }
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
            Assert.True(result[0].AmI);

            _sqlQueryRepoMock.Verify(x => x.SqlQueryListAsync<RawLeaderboardGlobalRankingDto>(
                It.IsAny<string>(), It.Is<NpgsqlParameter[]>(p =>
                    p.Length == 1 && Convert.ToInt32(p[0].Value) == 1)), Times.Once);

            _mapperMock.Verify(m => m.Map<List<LeaderboardGlobalRankingResponseDto>>(It.IsAny<List<RawLeaderboardGlobalRankingDto>>()), Times.Once);
        }

    }
}
