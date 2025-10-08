using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.WebAPI.Hubs;
using Xunit;
using static QuizVerse.Infrastructure.Common.Constants;

namespace QuizVerse.Tests.Hubs
{
    public class BattleHubTests
    {
        private readonly Mock<IBattleMatchmakingService> _mockBattleMatchmakingService;
        private readonly Mock<IBattleService> _mockBattleService;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IHubContext<BattleHub>> _mockHubContext;
        private readonly Mock<IGenericRepository<BattleStatus>> _mockBattleStatusRepo;
        private readonly Mock<IGenericRepository<BattleList>> _mockBattleListRepo;
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
            _mockBattleStatusRepo = new Mock<IGenericRepository<BattleStatus>>();
            _mockBattleListRepo = new Mock<IGenericRepository<BattleList>>();
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
                _mockBattleListRepo.Object,
                _mockMapper.Object)
            {
                Context = _mockContext.Object,
                Clients = _mockClients.Object,
                Groups = _mockGroups.Object
            };

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
            _mockHubContext.Setup(x => x.Clients.Client(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        }

        #region StartMatchmaking Tests

        [Fact]
        public async Task StartMatchmaking_WithCompletedBattle_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var completedBattle = new BattleStatus
            {
                Id = 100,
                BattleId = battleId,
                User1Id = userId,
                User2Id = 2,
                BattleStatus1 = (int)QuizVerse.Infrastructure.Enums.BattleStatus.Completed
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync(completedBattle);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == BATTLE_ALREADY_COMPLETED),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task StartMatchmaking_WithDrawBattle_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var drawBattle = new BattleStatus
            {
                Id = 100,
                BattleId = battleId,
                User1Id = userId,
                User2Id = 2,
                BattleStatus1 = (int)QuizVerse.Infrastructure.Enums.BattleStatus.Draw
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync(drawBattle);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == BATTLE_ALREADY_COMPLETED),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task StartMatchmaking_WhenMatchFound_CreatesBattleAndStartsGame()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

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
                Player1ConnectionId = "player1-conn",
                Player2ConnectionId = "player2-conn",
                TotalQuestions = 10
            };

            battleState.Connected.TryAdd(1, false);
            battleState.Connected.TryAdd(2, false);
            battleState.IsSkipInstruction.TryAdd(1, false);
            battleState.IsSkipInstruction.TryAdd(2, false);
            battleState.ActiveTimers.TryAdd(1, new CancellationTokenSource());
            battleState.ActiveTimers.TryAdd(2, new CancellationTokenSource());

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = 100,
                BattleName = "Test Battle",
                BattleDescription = "Test Description",
                BattleCategory = "Test Category",
                TimeInSeconds = 300
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync((BattleStatus?)null);

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, "test-connection-id"))
                .ReturnsAsync(matchResult);

            _mockBattleService.Setup(x => x.CreateBattleAsync(matchResult, battleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(battleState.BattleAttemptId))
                .ReturnsAsync(battleInstruction);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.MATCH_FOUND,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));

            _mockGroups.Verify(x => x.AddToGroupAsync("player1-conn", "100", It.IsAny<CancellationToken>()), Times.Once());
            _mockGroups.Verify(x => x.AddToGroupAsync("player2-conn", "100", It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.BATTLE_STARTED,
                It.Is<object[]>(args =>
                    args[0] is BattleStartDetails &&
                    ((BattleStartDetails)args[0]).BattleAttemptId == 100),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task StartMatchmaking_WhenBattleCreationFails_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

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

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync((BattleStatus?)null);

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, "test-connection-id"))
                .ReturnsAsync(matchResult);

            _mockBattleService.Setup(x => x.CreateBattleAsync(matchResult, battleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(battleState);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == FAILED_TO_CREATE_BATTLE),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task StartMatchmaking_WhenExceptionOccurs_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;

            // Mock BattleList to throw exception
            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == "Database error"),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task StartMatchmaking_AlreadyConnected_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var battleState = new BattleState
            {
                BattleAttemptId = battleId,
                Player1Id = userId
            };

            battleState.Connected.TryAdd(userId, true);

            BattleStateManager.AddBattle(battleId, battleState);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(battleId);
        }

        [Fact]
        public async Task StartMatchmaking_ResumeWithinWindow_SendsContinueBattle()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var battleStatus = new BattleStatus
            {
                Id = 100,
                BattleId = battleId,
                User1Id = userId,
                User2Id = 2,
                BattleStatus1 = (int)QuizVerse.Infrastructure.Enums.BattleStatus.Running
            };

            var battleState = new BattleState
            {
                BattleAttemptId = 100,
                Player1Id = userId,
                Player2Id = 2
            };

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-5));

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync(battleStatus);

            BattleStateManager.AddBattle(battleStatus.Id, battleState);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.CONTINUE_BATTLE,
                It.Is<object[]>(args => args[0].ToString().Contains("BattleAttemptId")),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(battleStatus.Id);
        }

        [Fact]
        public async Task StartMatchmaking_ResumeWindowExpired_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var battleStatus = new BattleStatus
            {
                Id = 100,
                BattleId = battleId,
                User1Id = userId,
                User2Id = 2,
                BattleStatus1 = (int)QuizVerse.Infrastructure.Enums.BattleStatus.Running
            };

            var battleState = new BattleState
            {
                BattleAttemptId = 100,
                Player1Id = userId,
                Player2Id = 2
            };

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-15));

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync(battleStatus);

            BattleStateManager.AddBattle(battleStatus.Id, battleState);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == BATTLE_SESSION_EXPIRED),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(battleStatus.Id);
        }

        [Fact]
        public async Task StartMatchmaking_RunningBattleNoState_SendsErrorMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var battleStatus = new BattleStatus
            {
                Id = 100,
                BattleId = battleId,
                User1Id = userId,
                User2Id = 2,
                BattleStatus1 = (int)QuizVerse.Infrastructure.Enums.BattleStatus.Running
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync(battleStatus);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == BATTLE_ALREADY_COMPLETED),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task StartMatchmaking_WaitingForOpponent_SendsSearchingMessage()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var matchResult = new MatchmakingResultDTO
            {
                IsMatched = false
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync((BattleStatus?)null);

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, "test-connection-id"))
                .ReturnsAsync(matchResult);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.SEARCHING,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once());
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

            var battleState = new BattleState
            {
                BattleAttemptId = battleId
            };

            // Initialize ConcurrentDictionary contents
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            BattleStateManager.AddBattle(battleId, battleState);

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_SCORE_UPDATE,
                It.Is<object[]>(args => args[0] is ScoreChangedDto),
                It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.LAST_ANSWERED_DETAIL,
                It.Is<object[]>(args => args[0] is LastAnswerdQuestionDetail),
                It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.Is<object[]>(args => args[0] is BattleQuestionResponseDto),
                It.IsAny<CancellationToken>()), Times.Once());

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

            var battleResult = new BattleResult
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
                It.Is<object[]>(args => args[0] is BattleCompletionResult),
                It.IsAny<CancellationToken>()), Times.Once());
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
                It.IsAny<CancellationToken>()), Times.Once());
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
                It.IsAny<CancellationToken>()), Times.Once());
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
                It.IsAny<CancellationToken>()), Times.Once());
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

            // Initialize ConcurrentDictionary contents
            battleState.Connected.TryAdd(userId, true);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }


        #endregion

        #region IntruptByPlayer Tests

        [Fact]
        public async Task IntruptByPlayer_WhenExceptionOccurs_SendsErrorMessage()
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

            _mockBattleService.Setup(x => x.IntruptByPlayer(attemptId, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Interrupt error"));

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.IntruptByPlayer(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == "Interrupt error"),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion

        #region CancelMatchmaking Tests

        [Fact]
        public async Task CancelMatchmaking_CallsServiceMethod()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Act
            await _hub.CancelMatchmaking(battleId);

            // Assert
            _mockBattleMatchmakingService.Verify(x => x.CancelMatchmaking(battleId, userId), Times.Once());
        }

        [Fact]
        public async Task CancelMatchmaking_UnauthorizedUser_ThrowsException()
        {
            // Arrange
            const int battleId = 1;

            _mockContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _hub.CancelMatchmaking(battleId));
        }

        #endregion

        #region OnDisconnectedAsync Tests

        [Fact]
        public async Task OnDisconnectedAsync_WithBattleId_CancelsMatchmaking()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ID, battleId } });

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockBattleMatchmakingService.Verify(x => x.CancelMatchmaking(battleId, userId), Times.Once());
        }

        [Fact]
        public async Task OnDisconnectedAsync_NoUserId_DoesNothing()
        {
            // Arrange
            _mockContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockBattleMatchmakingService.Verify(x => x.CancelMatchmaking(It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }

        [Fact]
        public async Task OnDisconnectedAsync_AlreadyDisconnected_DoesNothing()
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

            // Initialize ConcurrentDictionary contents
            battleState.Connected.TryAdd(userId, false);
            battleState.Connected.TryAdd(2, true);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            Assert.False(battleState.Connected[userId]); // Still false
            _mockBattleService.Verify(x => x.FinalizeBattleAsync(It.IsAny<BattleState>(), It.IsAny<CancellationToken>()), Times.Never());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_EmptyQuestionGivenTime_RemovesQuestion()
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

            // Initialize ConcurrentDictionary contents
            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, true);
            var questionDetail = new QuestionsDetail();
            questionDetail.QuestionGivenTime.TryAdd(userId, DateTime.UtcNow);
            battleState.AttemptedQuestionsDetails.Add(questionDetail);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            Assert.Empty(battleState.AttemptedQuestionsDetails);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }
        #endregion

        #region SkipInstructions Tests

        [Fact]
        public async Task SkipInstructions_ValidBattle_UpdatesState()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId
            };

            // Initialize ConcurrentDictionary contents
            battleState.IsSkipInstruction.TryAdd(userId, false);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.SkipInstructions(attemptId);

            // Assert
            Assert.True(battleState.IsSkipInstruction[userId]);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task SkipInstructions_NonExistentBattle_SendsErrorMessage()
        {
            // Arrange
            const int attemptId = 1;

            // Act
            await _hub.SkipInstructions(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == BATTLE_NOT_FOUND),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion

        #region WaitForBothPlayersReady Tests

        [Fact]
        public async Task WaitForBothPlayersReady_PlayerSkips_CompletesImmediately()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId
            };

            // Initialize ConcurrentDictionary contents
            battleState.IsSkipInstruction.TryAdd(userId, true);

            // Act
            var method = _hub.GetType().GetMethod("WaitForBothPlayersReady", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method); // Ensure method exists
            await (Task)method.Invoke(_hub, new object[] { battleState, userId });

            // Assert
            // No assertions needed; method should complete without delay
        }

        [Fact]
        public async Task WaitForBothPlayersReady_NoSkip_WaitsAndChecks()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId
            };

            // Initialize ConcurrentDictionary contents
            battleState.IsSkipInstruction.TryAdd(userId, false);

            // Act
            var method = _hub.GetType().GetMethod("WaitForBothPlayersReady", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method); // Ensure method exists
            var task = (Task)method.Invoke(_hub, new object[] { battleState, userId });

            // Simulate skip after a short delay
            await Task.Delay(50);
            battleState.IsSkipInstruction[userId] = true;
            await task;

            // Assert
            Assert.True(battleState.IsSkipInstruction[userId]);
        }
        #endregion

        #region Additional StartMatchmaking Tests

        [Fact]
        public async Task StartMatchmaking_WithMultipleActiveBattles_ChecksAllForConnection()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var battleState1 = new BattleState
            {
                BattleAttemptId = 100,
                Player1Id = userId,
                Player2Id = 2
            };
            battleState1.Connected.TryAdd(userId, false);

            var battleState2 = new BattleState
            {
                BattleAttemptId = 101,
                Player1Id = 3,
                Player2Id = userId
            };
            battleState2.Connected.TryAdd(userId, true);

            BattleStateManager.AddBattle(100, battleState1);
            BattleStateManager.AddBattle(101, battleState2);

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync((BattleStatus?)null);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(100);
            BattleStateManager.RemoveBattle(101);
        }

        [Fact]
        public async Task StartMatchmaking_StoresBattleIdInContext()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

            var matchResult = new MatchmakingResultDTO
            {
                IsMatched = false
            };

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync((BattleStatus?)null);

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, "test-connection-id"))
                .ReturnsAsync(matchResult);

            var contextItems = new Dictionary<object, object>();
            _mockContext.Setup(x => x.Items).Returns(contextItems);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            Assert.True(contextItems.ContainsKey(BATTLE_ID));
            Assert.Equal(battleId, contextItems[BATTLE_ID]);
        }

        [Fact]
        public async Task StartMatchmaking_StoresBattleAttemptIdInContext()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            // Mock BattleList first
            var battleList = new BattleList
            {
                Id = battleId,
                IsDeleted = false,
                BattleTimeLimited = false
            };

            _mockBattleListRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(battleList);

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
                Player1ConnectionId = "player1-conn",
                Player2ConnectionId = "player2-conn",
                TotalQuestions = 10
            };

            battleState.Connected.TryAdd(1, false);
            battleState.Connected.TryAdd(2, false);
            battleState.IsSkipInstruction.TryAdd(1, false);
            battleState.IsSkipInstruction.TryAdd(2, false);

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = 100,
                BattleName = "Test Battle"
            };

            var contextItems = new Dictionary<object, object>();
            _mockContext.Setup(x => x.Items).Returns(contextItems);

            _mockBattleStatusRepo
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                .ReturnsAsync((BattleStatus?)null);

            _mockBattleMatchmakingService.Setup(x => x.StartMatchmaking(battleId, userId, "test-connection-id"))
                .ReturnsAsync(matchResult);

            _mockBattleService.Setup(x => x.CreateBattleAsync(matchResult, battleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(battleState.BattleAttemptId))
                .ReturnsAsync(battleInstruction);

            // Act
            await _hub.StartMatchmaking(battleId);

            // Assert
            Assert.True(contextItems.ContainsKey(BATTLE_ATTEMPT_ID_KEY));
            Assert.Equal(100, contextItems[BATTLE_ATTEMPT_ID_KEY]);
        }

        #endregion

        #region Additional SubmitAnswer Tests

        [Fact]
        public async Task SubmitAnswer_WithOnlyScoreChanged_DoesNotSendOtherMessages()
        {
            // Arrange
            const int battleId = 1;
            const int questionIndex = 1;
            const string answer = "answer";
            const int userId = 1;

            var submitResult = new SubmitAnswerResult
            {
                ScoreChanged = new ScoreChangedDto(battleId, 10, 5, 1, 2, 2, 1, 1, 0)
            };

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_SCORE_UPDATE,
                It.Is<object[]>(args => args[0] is ScoreChangedDto),
                It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.LAST_ANSWERED_DETAIL,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Never());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task SubmitAnswer_WithoutNextQuestion_DoesNotStartNewTimer()
        {
            // Arrange
            const int battleId = 1;
            const int questionIndex = 1;
            const string answer = "answer";
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = battleId
            };
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());

            var submitResult = new SubmitAnswerResult
            {
                ScoreChanged = new ScoreChangedDto(battleId, 10, 5, 1, 2, 2, 1, 1, 0),
                LastAnswerdQuestionDetail = new LastAnswerdQuestionDetail(1, true, 5, "correct")
            };

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            BattleStateManager.AddBattle(battleId, battleState);
            var initialTimerCount = battleState.ActiveTimers.Count;

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert
            Assert.Equal(initialTimerCount, battleState.ActiveTimers.Count);

            // Cleanup
            BattleStateManager.RemoveBattle(battleId);
        }

        [Fact]
        public async Task SubmitAnswer_BattleStateNotFound_DoesNotStartTimer()
        {
            // Arrange
            const int battleId = 999; // Non-existent
            const int questionIndex = 1;
            const string answer = "answer";
            const int userId = 1;

            var submitResult = new SubmitAnswerResult
            {
                NextQuestion = new NextQuestionDto("test-connection-id", new BattleQuestionResponseDto
                {
                    QuestionIndex = 2,
                    QuizQuestionId = 2
                })
            };

            _mockBattleService.Setup(x => x.SubmitAnswerAsync(battleId, "test-connection-id", questionIndex, answer, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submitResult);

            // Act
            await _hub.SubmitAnswer(battleId, questionIndex, answer);

            // Assert - No exception thrown, gracefully handled
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion

        #region Additional ResumeBattle Tests

        [Fact]
        public async Task ResumeBattle_Player2Resume_UpdatesPlayer2ConnectionId()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 2;

            SetupMockContext(userId, "new-connection-id");

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = 1,
                Player2Id = userId,
                Player1ConnectionId = "player1-conn",
                Player2ConnectionId = "old-conn"
            };

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-5));
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());
            battleState.CurrentIndex.TryAdd(userId, 1);

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = attemptId,
                BattleName = "Test Battle"
            };

            var question = new BattleQuestionResponseDto
            {
                QuestionIndex = 1,
                QuizQuestionId = 1
            };

            BattleStateManager.AddBattle(attemptId, battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(attemptId))
                .ReturnsAsync(battleInstruction);

            _mockBattleMatchmakingService.Setup(x => x.GetPlayerProfile(It.IsAny<int>()))
                .ReturnsAsync(new PlayerProfileDTO());

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(battleState, "new-connection-id", userId, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(question);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            Assert.Equal("new-connection-id", battleState.Player2ConnectionId);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task ResumeBattle_NullQuestion_DoesNotSendQuestion()
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

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-5));
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());
            battleState.CurrentIndex.TryAdd(userId, 1);

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = attemptId,
                BattleName = "Test Battle"
            };

            BattleStateManager.AddBattle(attemptId, battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(attemptId))
                .ReturnsAsync(battleInstruction);

            _mockBattleMatchmakingService.Setup(x => x.GetPlayerProfile(It.IsAny<int>()))
                .ReturnsAsync(new PlayerProfileDTO());

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(battleState, "test-connection-id", userId, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BattleQuestionResponseDto?)null);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Never());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task ResumeBattle_CancelTimerThrowsException_ContinuesExecution()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var disposedCts = new CancellationTokenSource();
            disposedCts.Dispose();

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player2Id = 2
            };

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-5));
            battleState.ActiveTimers.TryAdd(userId, disposedCts);
            battleState.CurrentIndex.TryAdd(userId, 1);

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = attemptId,
                BattleName = "Test Battle"
            };

            BattleStateManager.AddBattle(attemptId, battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(attemptId))
                .ReturnsAsync(battleInstruction);

            _mockBattleMatchmakingService.Setup(x => x.GetPlayerProfile(It.IsAny<int>()))
                .ReturnsAsync(new PlayerProfileDTO());

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(It.IsAny<BattleState>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BattleQuestionResponseDto?)null);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.BATTLE_RESUMED,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion

        #region Additional OnDisconnectedAsync Tests

        [Fact]
        public async Task OnDisconnectedAsync_WithExceptionParameter_HandlesGracefully()
        {
            // Arrange
            const int battleId = 1;
            const int userId = 1;

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ID, battleId } });

            // Act
            await _hub.OnDisconnectedAsync(new Exception("Connection error"));

            // Assert
            _mockBattleMatchmakingService.Verify(x => x.CancelMatchmaking(battleId, userId), Times.Once());
        }

        [Fact]
        public async Task OnDisconnectedAsync_RemovesTimerFromActiveTimers()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var cts = new CancellationTokenSource();
            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player2Id = 2
            };

            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, true);
            battleState.ActiveTimers.TryAdd(userId, cts);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            Assert.False(battleState.ActiveTimers.ContainsKey(userId));

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_TimerCancelThrows_ContinuesExecution()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var cts = new CancellationTokenSource();
            cts.Dispose(); // Disposed CTS will throw on Cancel

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player2Id = 2
            };

            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, true);
            battleState.ActiveTimers.TryAdd(userId, cts);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act & Assert - Should not throw
            await _hub.OnDisconnectedAsync(null);

            Assert.False(battleState.Connected[userId]);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_MultipleQuestionsWithUserEntry_RemovesOnlyLast()
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

            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, true);

            var question1 = new QuestionsDetail();
            question1.QuestionGivenTime.TryAdd(userId, DateTime.UtcNow);
            question1.QuestionGivenTime.TryAdd(2, DateTime.UtcNow);

            var question2 = new QuestionsDetail();
            question2.QuestionGivenTime.TryAdd(userId, DateTime.UtcNow);

            battleState.AttemptedQuestionsDetails.Add(question1);
            battleState.AttemptedQuestionsDetails.Add(question2);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            Assert.Single(battleState.AttemptedQuestionsDetails); // Only question1 remains
            Assert.True(battleState.AttemptedQuestionsDetails[0].QuestionGivenTime.ContainsKey(2));

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion

        #region Additional SkipInstructions Tests

        [Fact]
        public async Task SkipInstructions_UpdatesSpecificUserFlag()
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

            battleState.IsSkipInstruction.TryAdd(userId, false);
            battleState.IsSkipInstruction.TryAdd(2, false);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.SkipInstructions(attemptId);

            // Assert
            Assert.True(battleState.IsSkipInstruction[userId]);
            Assert.False(battleState.IsSkipInstruction[2]); // Other player unaffected

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion

        #region IntruptByPlayer - Missing Coverage Tests

        [Fact]
        public async Task IntruptByPlayer_NonExistentBattle_SendsErrorMessage()
        {
            // Arrange
            const int attemptId = 1;

            // Act
            await _hub.IntruptByPlayer(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == NO_PENDING_BATTLE_TO_INTERRUPT),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task IntruptByPlayer_AlreadyCompleted_SendsErrorMessage()
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

            battleState.Completed.TryAdd(userId, true);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.IntruptByPlayer(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.ERROR,
                It.Is<object[]>(args => args[0].ToString() == YOU_HAVE_ALREADY_COMPLETED_THIS_BATTLE),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task IntruptByPlayer_PartialInterrupt_SendsCorrectMessages()
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

            battleState.Score.TryAdd(userId, 10);
            battleState.Score.TryAdd(2, 5);
            battleState.CurrentIndex.TryAdd(userId, 2);
            battleState.CurrentIndex.TryAdd(2, 1);
            battleState.Completed.TryAdd(userId, false);

            var questionDetail = new QuestionsDetail();
            questionDetail.IsCorrect.TryAdd(userId, true);
            questionDetail.IsCorrect.TryAdd(2, false);
            battleState.AttemptedQuestionsDetails.Add(questionDetail);

            _mockBattleService.Setup(x => x.IntruptByPlayer(attemptId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BattleFinishedDto?)null);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.IntruptByPlayer(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.PLAYER_INTERRUPTED,
                It.Is<object[]>(args => args[0].ToString().Contains("UserId")),
                It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.BATTLE_ENDED_FOR_PARTICULAR_PLAYER_DUE_TO_INTERRUPT,
                It.Is<object[]>(args => args[0].ToString().Contains("UserId")),
                It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_SCORE_UPDATE,
                It.Is<object[]>(args => args[0] is ScoreChangedDto),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task IntruptByPlayer_BothPlayersFinished_SendsBattleEnded()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleResult = new BattleResult
            {
                BattleStatus = attemptId,
                WinnerId = userId,
                WinnerGainedXp = 50,
                LooserGainedXp = 20
            };

            var battleCompletionResult = new BattleCompletionResult
            {
                BattleStatus = attemptId,
                WinnerId = userId,
                WinnerGainedXp = 50,
                LooserGainedXp = 20
            };

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player2Id = 2
            };

            battleState.Completed.TryAdd(userId, false);

            _mockBattleService.Setup(x => x.IntruptByPlayer(attemptId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BattleFinishedDto(attemptId, battleResult));

            _mockMapper.Setup(x => x.Map<BattleCompletionResult>(battleResult))
                .Returns(battleCompletionResult);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.IntruptByPlayer(attemptId);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.PLAYER_INTERRUPTED,
                It.Is<object[]>(args => args[0].ToString().Contains("UserId")),
                It.IsAny<CancellationToken>()), Times.Once());

            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.BATTLE_ENDED,
                It.Is<object[]>(args => args[0] is BattleCompletionResult),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }
        #endregion

        #region ResumeBattle - Missing Coverage Tests

        [Fact]
        public async Task ResumeBattle_Player1Resume_UpdatesPlayer1ConnectionId()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player2Id = 2,
                Player1ConnectionId = "old-conn",
                Player2ConnectionId = "player2-conn"
            };

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-5));
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());
            battleState.CurrentIndex.TryAdd(userId, 1);

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = attemptId,
                BattleName = "Test Battle"
            };

            BattleStateManager.AddBattle(attemptId, battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(attemptId))
                .ReturnsAsync(battleInstruction);

            _mockBattleMatchmakingService.Setup(x => x.GetPlayerProfile(It.IsAny<int>()))
                .ReturnsAsync(new PlayerProfileDTO());

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(It.IsAny<BattleState>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BattleQuestionResponseDto?)null);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Assert
            Assert.Equal("test-connection-id", battleState.Player1ConnectionId);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task ResumeBattle_WithQuestion_SendsQuestionAndStartsTimer()
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

            battleState.Connected.TryAdd(userId, false);
            battleState.ConnectionBrokeTime.TryAdd(userId, DateTime.UtcNow.AddMinutes(-5));
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());
            battleState.CurrentIndex.TryAdd(userId, 1);

            var battleInstruction = new BattleInstructionDTO
            {
                BattleAttemptId = attemptId,
                BattleName = "Test Battle"
            };

            var question = new BattleQuestionResponseDto
            {
                QuestionIndex = 1,
                QuizQuestionId = 1,
                QuestionName = "Test Question"
            };

            BattleStateManager.AddBattle(attemptId, battleState);

            _mockBattleService.Setup(x => x.GetBattleInstructions(attemptId))
                .ReturnsAsync(battleInstruction);

            _mockBattleMatchmakingService.Setup(x => x.GetPlayerProfile(It.IsAny<int>()))
                .ReturnsAsync(new PlayerProfileDTO());

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(It.IsAny<BattleState>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(question);

            // Act
            await _hub.ResumeBattle(attemptId);

            // Wait for async operations
            await Task.Delay(100);

            // Assert
            _mockClientProxy.Verify(x => x.SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.Is<object[]>(args => args[0] is BattleQuestionResponseDto),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion

        #region OnDisconnectedAsync - Missing Coverage Tests

        [Fact]
        public async Task OnDisconnectedAsync_FallbackFindsBattle_UpdatesState()
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

            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, true);
            battleState.ActiveTimers.TryAdd(userId, new CancellationTokenSource());

            BattleStateManager.AddBattle(attemptId, battleState);

            // Don't set BATTLE_ATTEMPT_ID_KEY to force fallback path
            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object>());

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            Assert.False(battleState.Connected[userId]);
            Assert.True(battleState.ConnectionBrokeTime.ContainsKey(userId));

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_BothPlayersDisconnected_TriggersCleanup()
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

            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, false);

            var scope = new Mock<IServiceScope>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IBattleService)))
                .Returns(_mockBattleService.Object);
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

            BattleStateManager.AddBattle(attemptId, battleState);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Short delay to allow async cleanup to start
            await Task.Delay(100);

            // Assert
            Assert.False(battleState.Connected[userId]);

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        [Fact]
        public async Task OnDisconnectedAsync_QuestionWithMultipleUsers_OnlyRemovesDisconnectedUser()
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

            battleState.Connected.TryAdd(userId, true);
            battleState.Connected.TryAdd(2, true);

            var questionDetail = new QuestionsDetail();
            questionDetail.QuestionGivenTime.TryAdd(userId, DateTime.UtcNow);
            questionDetail.QuestionGivenTime.TryAdd(2, DateTime.UtcNow);
            battleState.AttemptedQuestionsDetails.Add(questionDetail);

            _mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object> { { BATTLE_ATTEMPT_ID_KEY, attemptId } });

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            Assert.Single(battleState.AttemptedQuestionsDetails);
            Assert.False(battleState.AttemptedQuestionsDetails[0].QuestionGivenTime.ContainsKey(userId));
            Assert.True(battleState.AttemptedQuestionsDetails[0].QuestionGivenTime.ContainsKey(2));

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion

        #region CountdownToStart - Missing Coverage Tests

        [Fact]
        public async Task CountdownToStart_SendsQuestionAfterCountdown()
        {
            // Arrange
            const int attemptId = 1;
            const int userId = 1;

            var battleState = new BattleState
            {
                BattleAttemptId = attemptId,
                Player1Id = userId,
                Player1ConnectionId = "test-connection-id"
            };

            battleState.IsSkipInstruction.TryAdd(userId, true);

            var question = new BattleQuestionResponseDto
            {
                QuestionIndex = 1,
                QuizQuestionId = 1
            };

            var scope = new Mock<IServiceScope>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IBattleService)))
                .Returns(_mockBattleService.Object);
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

            _mockBattleService.Setup(x => x.GetQuestionForPlayerAsync(battleState, "test-connection-id", userId, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(question);

            BattleStateManager.AddBattle(attemptId, battleState);

            // Act
            var method = _hub.GetType().GetMethod("CountdownToStart", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method.Invoke(_hub, new object[] { battleState, userId });

            // Short delay for async operations
            await Task.Delay(100);

            // Assert
            _mockHubContext.Verify(x => x.Clients.Client("test-connection-id").SendCoreAsync(
                SignalRMethods.RECEIVE_QUESTION,
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once());

            // Cleanup
            BattleStateManager.RemoveBattle(attemptId);
        }

        #endregion
    }
}