using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using AutoMapper;
using Xunit;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using System.Linq.Expressions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Common.Exceptions;

namespace QuizVerse.UnitTests.Services
{
    public class UserBattlesServiceTest
    {
        private readonly Mock<IGenericRepository<BattleList>> _mockBattleListRepository;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly Mock<IGenericRepository<BattleRequest>> _mockBattleRequestRepository;
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

            _mockBattleListRepository = new Mock<IGenericRepository<BattleList>>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();
            _mockBattleRequestRepository = new Mock<IGenericRepository<BattleRequest>>();
            _mockSqlQueryRepository = new Mock<ISqlQueryRepository>();
            _mockMapper = new Mock<IMapper>();

            _service = new UserBattlesService(
                _mockBattleListRepository.Object,
                _mockUserRepository.Object,
                _mockBattleRequestRepository.Object,
                _mockHttpContextAccessor.Object,
                _mockSqlQueryRepository.Object,
                _mockMapper.Object);
        }

        #region User Available Battles
        [Fact]
        public async Task GetUserAvailableBattles_ReturnsAvailableBattlesList()
        {
            var rawAvailableBattles = new List<UserAvailableBattleDto>
            {
                new()
                {
                    BattleId = 1,
                    BattleName = "Battle1",
                    Category = "Strategy",
                    Difficulty = "Medium",
                    Description = "Test Battle Description",
                    MaxXP = 500,
                    TotalQuestions = 10,
                    Duration = TimeSpan.FromMinutes(15),
                    Participants = 5
                }
            };

            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQueryListAsync<UserAvailableBattleDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter>()))
                .ReturnsAsync(rawAvailableBattles);

            var result = await _service.GetUserAvailableBattles();

            Assert.NotNull(result);
            Assert.Single(result);

            var battle = result[0];
            Assert.Equal(1, battle.BattleId);
            Assert.Equal("Battle1", battle.BattleName);
            Assert.Equal("Strategy", battle.Category);
            Assert.Equal("Medium", battle.Difficulty);
            Assert.Equal("Test Battle Description", battle.Description);
            Assert.Equal(500, battle.MaxXP);
            Assert.Equal(10, battle.TotalQuestions);
            Assert.Equal(TimeSpan.FromMinutes(15), battle.Duration);
            Assert.Equal(5, battle.Participants);

            _mockSqlQueryRepository.Verify(repo =>
                repo.SqlQueryListAsync<UserAvailableBattleDto>(
                    It.IsAny<string>(),
                    It.Is<NpgsqlParameter>(p => p.ParameterName == "p_user_id" && (int)p.Value == _service.UserId)), Times.Once);
        }
        #endregion

        #region IsUserNameExist Tests
        [Fact]
        public async Task IsUserNameExist_UserDoesNotExist_ThrowsAppException()
        {
            string testUserName = "nonexistent";
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<AppException>(
                () => _service.CheckUserExistence(testUserName)
            );

            Assert.Equal(Constants.USERNAME_DOES_NOT_EXIST, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task IsUserNameExist_UserIsSelf_ThrowsAppException()
        {
            string testUserName = "selfuser";
            var user = new User { Id = 2, UserName = testUserName }; // same as UserId in context

            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(user);

            var exception = await Assert.ThrowsAsync<AppException>(
                () => _service.CheckUserExistence(testUserName)
            );

            Assert.Equal(Constants.SELF_CHALLENGE_NOT_ALLOWED, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task IsUserNameExist_UserExists_ReturnsTrue()
        {
            string testUserName = "otheruser";
            var user = new User { Id = 99, UserName = testUserName }; // different from current UserId

            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(user);

            var result = await _service.CheckUserExistence(testUserName);

            Assert.True(result);
        }
        #endregion

        #region User Recent Battles
        [Fact]
        public async Task GetUserBattleHistory_ReturnsMappedBattleList()
        {
            var dto = new UserBattleHistoryRequestDto
            {
                BatchNumber = 1,
                FilterBy = null,
                TimeFilterBy = null
            };

            var rawResult = new UserBattleHistoryRawResult
            {
                Battles = "[{\"Opponent\":\"opponent 1\",\"Category\":\"general knowledge\",\"Result\":\"Won\",\"YourScore\":8,\"OpponentScore\":6,\"XpGained\":10}]",
                HasMore = false
            };

            var initialMappedResponse = new UserBattleHistoryResponseDto
            {
                HasMore = false,
                Battles = new List<UserBattleHistoryDto>
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
                }
            };

            var finalMappedBattle = new UserBattleHistoryDto
            {
                Opponent = "Opponent 1",
                Category = "General Knowledge",
                Result = "Won",
                YourScore = 8,
                OpponentScore = 6,
                XpGained = 10
            };

            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQuerySingleAsync<UserBattleHistoryRawResult>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(rawResult);

            _mockMapper
                .Setup(m => m.Map<UserBattleHistoryResponseDto>(rawResult))
                .Returns(initialMappedResponse);

            _mockMapper
                .Setup(m => m.Map<UserBattleHistoryDto>(It.Is<UserBattleHistoryDto>(
                    b => b.Opponent == "opponent 1")))
                .Returns(finalMappedBattle);

            var result = await _service.GetUserBattleHistory(dto);

            Assert.NotNull(result);
            Assert.False(result.HasMore);
            Assert.Single(result.Battles);
            Assert.Equal("Opponent 1", result.Battles[0].Opponent);
            Assert.Equal("General Knowledge", result.Battles[0].Category);
            Assert.Equal(8, result.Battles[0].YourScore);

            _mockSqlQueryRepository.Verify(repo =>
                repo.SqlQuerySingleAsync<UserBattleHistoryRawResult>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()),
                Times.Once);

            _mockMapper.Verify(m =>
                m.Map<UserBattleHistoryResponseDto>(rawResult),
                Times.Once);

            _mockMapper.Verify(m =>
                m.Map<UserBattleHistoryDto>(It.IsAny<UserBattleHistoryDto>()),
                Times.Exactly(initialMappedResponse.Battles.Count));
        }
        #endregion

        #region Battle Leaderboard Data
        [Fact]
        public async Task GetBattleLeaderboardList_ReturnsMappedLeaderboardList()
        {
            // Arrange
            var rawLeaderboardData = new List<UserBattleLeaderboardData>
            {
                new()
                {
                    UserName = "User1",
                    TotalWins = 5,
                    WinPercentage = 50.0m,
                    TotalXp = 1200,
                    Rank = 1,
                    IsLoggedInUser = true
                }
            };

            var mappedLeaderboardData = new List<UserBattleLeaderboardData>
            {
                new()
                {
                    UserName = "User1",
                    TotalWins = 5,
                    WinPercentage = 50.0m,
                    TotalXp = 1200,
                    Rank = 1,
                    IsLoggedInUser = true
                }
            };

            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQueryListAsync<UserBattleLeaderboardData>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(rawLeaderboardData);

            _mockMapper
                .Setup(m => m.Map<List<UserBattleLeaderboardData>>(rawLeaderboardData))
                .Returns(mappedLeaderboardData);

            // Act
            var result = await _service.GetBattleLeaderboardList();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("User1", result[0].UserName);
            Assert.Equal(5, result[0].TotalWins);
            Assert.Equal(50.0m, result[0].WinPercentage);
            Assert.Equal(1200, result[0].TotalXp);
            Assert.Equal(1, result[0].Rank);
            Assert.True(result[0].IsLoggedInUser);

            _mockSqlQueryRepository.Verify(repo =>
                repo.SqlQueryListAsync<UserBattleLeaderboardData>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Once);

            _mockMapper.Verify(mapper =>
                mapper.Map<List<UserBattleLeaderboardData>>(rawLeaderboardData), Times.Once);
        }
        #endregion

        #region Battle Leaderboard Data
        [Fact]
        public async Task SendBattleRequest_BattleDoesNotExist_ThrowsAppException()
        {
            // Arrange
            var dto = new SendBattleRequestDTO { BattleId = 1, ReceiverUsername = "receiver" };
            _mockBattleListRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AppException>(() => _service.SendBattleRequest(dto));
            Assert.Equal(Constants.BATTLE_NOT_FOUND, exception.Message);
            Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task SendBattleRequest_ReceiverUserNotFound_ThrowsAppException()
        {
            var dto = new SendBattleRequestDTO { BattleId = 1, ReceiverUsername = "receiver" };
            _mockBattleListRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>()))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<AppException>(() => _service.SendBattleRequest(dto));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, exception.Message);
            Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task SendBattleRequest_SelfChallenge_ThrowsAppException()
        {
            var dto = new SendBattleRequestDTO { BattleId = 1, ReceiverUsername = "receiver" };
            var receiver = new User { Id = 2, UserName = "receiver" };

            _mockBattleListRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>()))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(receiver);

            var exception = await Assert.ThrowsAsync<AppException>(() => _service.SendBattleRequest(dto));
            Assert.Equal(Constants.SELF_CHALLENGE_NOT_ALLOWED, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task SendBattleRequest_BattleAlreadyAccepted_ThrowsAppException()
        {
            var dto = new SendBattleRequestDTO { BattleId = 1, ReceiverUsername = "receiver" };

            var receiver = new User { Id = 99, UserName = "receiver" };

            _mockBattleListRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>()))
                .ReturnsAsync(true);

            _mockUserRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(receiver);

            _mockBattleRequestRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleRequest, bool>>>()))
                .ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<AppException>(() => _service.SendBattleRequest(dto));

            Assert.Equal(Constants.BATTLE_ALREADY_ACCEPTED, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task SendBattleRequest_ActiveChallengeExists_ThrowsAppException()
        {
            var dto = new SendBattleRequestDTO { BattleId = 1, ReceiverUsername = "receiver" };
            var receiver = new User { Id = 99, UserName = "receiver" };

            _mockBattleListRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>()))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(receiver);
            _mockBattleRequestRepository.SetupSequence(r => r.Exists(It.IsAny<Expression<Func<BattleRequest, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<AppException>(() => _service.SendBattleRequest(dto));
            Assert.Equal(Constants.ACTIVE_CHALLENGE_EXISTS, exception.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Fact]
        public async Task SendBattleRequest_ValidRequest_ReturnsSuccessMessage()
        {
            var dto = new SendBattleRequestDTO { BattleId = 1, ReceiverUsername = "receiver" };
            var receiver = new User { Id = 99, UserName = "receiver" };

            _mockBattleListRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>()))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(receiver);
            _mockBattleRequestRepository.SetupSequence(r => r.Exists(It.IsAny<Expression<Func<BattleRequest, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false);
            _mockBattleRequestRepository.Setup(r => r.AddAsync(It.IsAny<BattleRequest>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(m => m.Map<BattleRequest>(It.IsAny<SendBattleRequestDTO>()))
                .Returns(new BattleRequest());

            var result = await _service.SendBattleRequest(dto);

            _mockBattleRequestRepository.Verify(r => r.AddAsync(It.Is<BattleRequest>(br =>
                br.SenderId == 2 && br.ReceiverId == receiver.Id)), Times.Once);
            Assert.Equal(Constants.BATTLE_REQUEST_SENT_SUCCESS, result);
        }
        #endregion

        #region Get Battle Result
        [Fact]
        public async Task GetBattleResult_ReturnsBattleResult_WhenDataExists()
        {
            // Arrange
            int battleId = 10;
            var expectedResult = new UserBattleResult
            {
                BattleName = "General Knowledge Battle",
                OpponentUserName = "Opponent1",
                PlayerProfile = "player.png",
                OpponentProfile = "opponent.png",
                BattleStatus = 2,
                IsWin = true,
                PlayerAttemptedQuestions = 8,
                OpponentAttemptedQuestions = 6,
                PlayerEarnedXP = 100
            };

            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQuerySingleAsync<UserBattleResult>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(expectedResult);

            _mockMapper
                .Setup(m => m.Map<UserBattleResult>(expectedResult))
                .Returns(expectedResult);

            // Act
            var result = await _service.GetBattleResult(battleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("General Knowledge Battle", result.BattleName);
            Assert.Equal("Opponent1", result.OpponentUserName);
            Assert.True(result.IsWin);
            Assert.Equal(100, result.PlayerEarnedXP);

            _mockSqlQueryRepository.Verify(repo =>
                repo.SqlQuerySingleAsync<UserBattleResult>(
                    It.IsAny<string>(),
                    It.Is<NpgsqlParameter[]>(p =>
                        p.Any(x => x.ParameterName == "p_battle_id" && (int)x.Value == battleId) &&
                        p.Any(x => x.ParameterName == "p_login_user_id" && (int)x.Value == _service.UserId) &&
                        p.Any(x => x.ParameterName == "p_status_running"))),
                Times.Once);
        }

        [Fact]
        public async Task GetBattleResult_WhenBattleNotFound_ThrowsAppException()
        {
            // Arrange
            int battleId = 999;
            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQuerySingleAsync<UserBattleResult>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ThrowsAsync(new AppException("Battle not found.", StatusCodes.Status404NotFound));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.GetBattleResult(battleId));
            Assert.Equal("Battle not found.", ex.Message);
            Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        }

        [Fact]
        public async Task GetBattleResult_WhenBattleStillRunning_ThrowsAppException()
        {
            // Arrange
            int battleId = 100;
            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQuerySingleAsync<UserBattleResult>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ThrowsAsync(new AppException("Battle is still running.", StatusCodes.Status400BadRequest));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.GetBattleResult(battleId));
            Assert.Equal("Battle is still running.", ex.Message);
            Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        }

        [Fact]
        public async Task GetBattleResult_WhenResultNotFound_ThrowsAppException()
        {
            // Arrange
            int battleId = 101;
            _mockSqlQueryRepository
                .Setup(repo => repo.SqlQuerySingleAsync<UserBattleResult>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ThrowsAsync(new AppException("Battle result not found.", StatusCodes.Status404NotFound));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.GetBattleResult(battleId));
            Assert.Equal("Battle result not found.", ex.Message);
            Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        }
        #endregion
    }
}