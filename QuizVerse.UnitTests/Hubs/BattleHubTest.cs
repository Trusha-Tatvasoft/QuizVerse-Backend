using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using AutoMapper;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.WebAPI.Hubs;
using static QuizVerse.Infrastructure.Common.Constants;
using QuizVerse.Infrastructure.Common.Helper;
using System.Linq.Expressions;
using QuizVerse.Domain.Entities;

namespace QuizVerse.Tests.Hubs
{
    public class BattleHubTests
    {
        private readonly Mock<IBattleMatchmakingService> _mockBattleMatchmakingService;
        private readonly Mock<IBattleService> _mockBattleService;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IHubContext<BattleHub>> _mockHubContext;
        private readonly Mock<IGenericRepository<QuizVerse.Domain.Entities.BattleStatus>> _mockBattleStatusRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly BattleHub _hub;

        public BattleHubTests()
        {
            _mockBattleMatchmakingService = new Mock<IBattleMatchmakingService>();
            _mockBattleService = new Mock<IBattleService>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockHubContext = new Mock<IHubContext<BattleHub>>();
            _mockBattleStatusRepo = new Mock<IGenericRepository<QuizVerse.Domain.Entities.BattleStatus>>();
            _mockMapper = new Mock<IMapper>();
            _mockContext = new Mock<HubCallerContext>();
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();
            _mockGroups = new Mock<IGroupManager>();

            _hub = new BattleHub(
                _mockBattleMatchmakingService.Object,
                _mockBattleService.Object,
                _mockScopeFactory.Object,
                _mockHubContext.Object,
                _mockBattleStatusRepo.Object,
                _mockMapper.Object);

            SetupMockContext();
        }

        private void SetupMockContext(int userId = 1, string connectionId = "test-connection-id")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, userId.ToString())], "mock"));

            _mockContext.Setup(x => x.User).Returns(httpContext.User);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object>());

            _mockClients.Setup(x => x.Client(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _hub.Context = _mockContext.Object;
            _hub.Clients = _mockClients.Object;
            _hub.Groups = _mockGroups.Object;
        }

        #region StartMatchmaking Tests

        [Fact]
        public async Task StartMatchmaking_WithCompletedBattle_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            var completedBattle = new QuizVerse.Domain.Entities.BattleStatus
            {
                Id = 100,
                BattleId = battleId,
                User1Id = userId,
                User2Id = 2,
                BattleStatus1 = (int)QuizVerse.Infrastructure.Enums.BattleStatus.Completed
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<BattleStatus, bool>>>(),
                    It.IsAny<Func<IQueryable<BattleStatus>, IQueryable<BattleStatus>>?>()
                ))
                .ReturnsAsync(completedBattle);


            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == BATTLE_ALREADY_COMPLETED),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartMatchmaking_WhenMatchFound_CreatesBattleAndStartsGame()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<QuizVerse.Domain.Entities.BattleStatus, bool>>>(),
                    It.IsAny<Func<IQueryable<QuizVerse.Domain.Entities.BattleStatus>, IQueryable<QuizVerse.Domain.Entities.BattleStatus>>?>()
                ))
                .ReturnsAsync((QuizVerse.Domain.Entities.BattleStatus?)null);


            var matchResult = new MatchmakingResultDTO
            {
                IsMatched = true,
                Player = new MatchmakingPlayerDTO { UserId = 1, ConnectionId = "player1-conn" },
                Opponent = new MatchmakingPlayerDTO { UserId = 2, ConnectionId = "player2-conn" },
                PlayerProfile = new PlayerProfileDTO { UserId = 1, UserName = "Player1" },
                OpponentProfile = new PlayerProfileDTO { UserId = 2, UserName = "Player2" }
            };

            var battleState = new BattleState
            {
                BattleAttemptId = 100,
                Player1Id = 1,
                Player2Id = 2,
                TotalQuestions = 10
            };

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = 100,
                BattleName = "Test Battle",
                BattleDescription = "Test Description",
                BattleCategory = "Test Category",
                TimeInSeconds = 300
            };

            var question = new BattleQuestionResponseDto
            {
                QuestionIndex = 1,
                QuizQuestionId = 1,
                QuestionName = "Test Question",
                QuestionType = "MCQ",
                Options = new List<OptionResponseDto>(),
                TimeInSeconds = 30
            };

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, It.IsAny<string>()))
                .ReturnsAsync(matchResult);

            _mockBattleService.Setup(x => x.CreateBattleAsync(matchResult, battleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(battleState.BattleAttemptId))
                .ReturnsAsync(battleInstruction);

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(battleState, It.IsAny<string>(), It.IsAny<int>(), 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(question);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.MATCH_FOUND,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for each player

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.BATTLE_STARTED,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockGroups.Verify(x => x.AddToGroupAsync("player1-conn", "100", It.IsAny<CancellationToken>()), Times.Once);
            _mockGroups.Verify(x => x.AddToGroupAsync("player2-conn", "100", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartMatchmaking_WhenBattleCreationFails_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<QuizVerse.Domain.Entities.BattleStatus, bool>>>(),
                    It.IsAny<Func<IQueryable<QuizVerse.Domain.Entities.BattleStatus>, IQueryable<QuizVerse.Domain.Entities.BattleStatus>>?>()
                ))
                .ReturnsAsync((QuizVerse.Domain.Entities.BattleStatus?)null);


            var matchResult = new MatchmakingResultDTO
            {
                IsMatched = true,
                Player = new MatchmakingPlayerDTO { UserId = 1, ConnectionId = "player1-conn" },
                Opponent = new MatchmakingPlayerDTO { UserId = 2, ConnectionId = "player2-conn" }
            };

            var battleState = new BattleState
            {
                BattleAttemptId = 0 // Invalid battle attempt ID
            };

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, It.IsAny<string>()))
                .ReturnsAsync(matchResult);

            _mockBattleService.Setup(x => x.CreateBattleAsync(matchResult, battleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(battleState);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == FAILED_TO_CREATE_BATTLE),
                It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for each player
        }

        [Fact]
        public async Task StartMatchmaking_WhenExceptionOccurs_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(
                    It.IsAny<Expression<Func<BattleStatus, bool>>>(),
                    It.IsAny<Func<IQueryable<BattleStatus>, IQueryable<BattleStatus>>?>()
                ))
                .ThrowsAsync(new Exception("Database error"));


            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == "Database error"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SubmitAnswer Tests

        [Fact]
        public async Task SubmitAnswer_WithValidAnswer_ProcessesSuccessfully()
        {
            // Arrange
            const int battleId = 1;
            const int questionIndex = 1;
            const string answer = "correct answer";
            const int userId = 1;

            var submitResult = new SubmitAnswerResult
            {
                ScoreChanged = new ScoreChangedDto(battleId, 10, 5, 1, 2, 2, 1, 1, 0),
                NextQuestion = new NextQuestionDto("test-connection-id", new BattleQuestionResponseDto
                {
                    QuestionIndex = 2,
                    QuizQuestionId = 2,
                    QuestionName = "Next Question",
                    TimeInSeconds = 30
                }),
                LastAnswerdQuestionDetail = new LastAnswerdQuestionDetail(1, true, 5, "correct answer")
            };

            var battleState = new BattleState { BattleAttemptId = battleId };

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            BattleStateManager.AddBattle(battleId, battleState);

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_SCORE_UPDATE,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.LAST_ANSWERED_DETAIL,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            // Cleanup
            BattleStateManager.RemoveBattle(battleId);
        }

        [Fact]
        public async Task SubmitAnswer_WithBattleFinished_SendsBattleEndedMessage()
        {
            // Arrange
            const int battleId = 1;
            const int questionIndex = 1;
            const string answer = "correct answer";
            const int userId = 1;

            var battleResult = new QuizVerse.Domain.Entities.BattleResult
            {
                BattleStatus = battleId,
                WinnerId = userId,
                WinnerGainedXp = 50,
                LooserGainedXp = 20
            };

            var battleCompletionResult = new BattleCompletionResult
            {
                BattleStatus = battleId,
                WinnerId = userId,
                WinnerGainedXp = 50,
                LooserGainedXp = 20
            };

            var submitResult = new SubmitAnswerResult
            {
                Finished = new BattleFinishedDto(battleId, battleResult)
            };

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            _mockMapper.Setup(x => x.Map<BattleCompletionResult>(battleResult))
                .Returns(battleCompletionResult);

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.BATTLE_ENDED,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_WithErrorMessage_SendsErrorToClient()
        {
            // Arrange
            const int battleId = 1;
            const int questionIndex = 1;
            const string answer = "answer";
            const int userId = 1;

            var submitResult = new SubmitAnswerResult
            {
                ErrorMessage = "Invalid question index"
            };

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == "Invalid question index"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_WhenExceptionOccurs_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int questionIndex = 1;
            const string answer = "answer";

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == "Service error"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region ResumeBattle Tests

        [Fact]
        public async Task ResumeBattle_WithNonExistentBattle_SendsErrorMessage()
        {
            // Arrange
            const int attemptId = 1;

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == NO_PENDING_BATTLE_TO_RESUME),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResumeBattle_WithAlreadyConnectedUser_SendsErrorMessage()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player2Id = 2
            };
            battleState.Connected[userId] = true;

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE),
                It.IsAny<CancellationToken>()), Times.Once);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }
        #endregion
    }
}