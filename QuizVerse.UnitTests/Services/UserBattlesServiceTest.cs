using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using AutoMapper;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class UserBattlesServiceTest
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ISqlQueryRepository> _mockSqlQueryRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserBattlesService _service;

        public UserBattlesServiceTest()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, "2")], "mock"));

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            _mockSqlQueryRepository = new Mock<ISqlQueryRepository>();
            _mockMapper = new Mock<IMapper>();

            _service = new UserBattlesService(
                _mockHttpContextAccessor.Object,
                _mockSqlQueryRepository.Object,
                _mockMapper.Object);
        }

        [Fact]
        public async Task GetUserRecentBattles_ReturnsMappedBattleList()
        {
            var rawBattles = new List<UserRecentBattleDto>
            {
                new()
                {
                    Opponent = "opponent 1",
                    Category = "general knowledge",
                    Result = "Won",
                    YourScore = 8,
                    OpponentScore = 6,
                    XpGained = 10
                }
            };

            var mappedBattles = new List<UserRecentBattleDto>
            {
                new()
                {
                    Opponent = "Opponent 1",
                    Category = "General Knowledge",
                    Result = "Won",
                    YourScore = 8,
                    OpponentScore = 6,
                    XpGained = 10
                }
            };

            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQueryListAsync<UserRecentBattleDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(rawBattles);

            _mockMapper
                .Setup(m => m.Map<List<UserRecentBattleDto>>(rawBattles))
                .Returns(mappedBattles);

            var result = await _service.GetUserRecentBattles();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Opponent 1", result[0].Opponent);
            Assert.Equal("General Knowledge", result[0].Category);
            Assert.Equal(8, result[0].YourScore);

            _mockSqlQueryRepository.Verify(repo =>
                repo.SqlQueryListAsync<UserRecentBattleDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Once);

            _mockMapper.Verify(mapper =>
                mapper.Map<List<UserRecentBattleDto>>(rawBattles), Times.Once);
        }
    }
}
