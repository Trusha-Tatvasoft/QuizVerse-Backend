using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class BattleServiceTests : IDisposable
    {
        private readonly Mock<IAiService> _aiMock = new();
        private readonly Mock<IGenericRepository<BaseQuestion>> _baseQRepo = new();
        private readonly Mock<IGenericRepository<BattleResult>> _battleResultRepo = new();
        private readonly Mock<IGenericRepository<BattleStatus>> _battleStatusRepo = new();
        private readonly Mock<IGenericRepository<BattleList>> _battleListRepo = new();
        private readonly Mock<IGenericRepository<QuizToBaseQuestionMap>> _quizMapRepo = new();
        private readonly Mock<ISqlQueryRepository> _sqlRepo = new();
        private readonly Mock<IMapper> _mapper = new();

        public BattleServiceTests()
        {
            // By default ensure repository Add/Update calls succeed
            _battleStatusRepo.Setup(x => x.AddAsync(It.IsAny<BattleStatus>())).Returns(Task.CompletedTask);
            _battleStatusRepo.Setup(x => x.UpdateAsync(It.IsAny<BattleStatus>())).Returns(Task.CompletedTask);
            _battleResultRepo.Setup(x => x.AddAsync(It.IsAny<BattleResult>())).Returns(Task.CompletedTask);
            _battleListRepo
                .Setup(x => x.GetAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<BattleList, bool>>>(),
                    It.IsAny<Func<IQueryable<BattleList>, IQueryable<BattleList>>?>()
                ))
                .ReturnsAsync(new BattleList { Id = 1, QuizId = 10 });

        }

        private BattleService CreateService()
        {
            return new BattleService(
                _aiMock.Object,
                _baseQRepo.Object,
                _battleResultRepo.Object,
                _battleStatusRepo.Object,
                _battleListRepo.Object,
                _quizMapRepo.Object,
                _mapper.Object,
                _sqlRepo.Object
            );
        }

        public void Dispose()
        {
            // Cleanup global state after each test
            foreach (var b in BattleStateManager.GetAllBattles().ToList())
            {
                BattleStateManager.RemoveBattle(b.BattleAttemptId);
            }
        }

        private BattleState CreateSimpleState(int battleAttemptId, int p1Id = 1, int p2Id = 2, int totalQuestions = 3)
        {
            var state = new BattleState
            {
                BattleAttemptId = battleAttemptId,
                Player1ConnectionId = "c1",
                Player2ConnectionId = "c2",
                TotalQuestions = totalQuestions
            };
            state.Player1Id = p1Id;
            state.Player2Id = p2Id;

            state.CurrentIndex[p1Id] = 1;
            state.Score[p1Id] = 0;
            state.Completed[p1Id] = false;

            state.CurrentIndex[p2Id] = 1;
            state.Score[p2Id] = 0;
            state.Completed[p2Id] = false;

            // prefill attempted questions with defaults
            for (int i = 0; i < totalQuestions; i++)
            {
                var qd = new QuestionsDetail
                {
                    Index = i + 1,
                    QuestionId = 100 + i,
                    Xp = 10,
                    TimeLimit = 30,

                };
                qd.QuestionGivenTime.TryAdd(p1Id, DateTime.MinValue);
                qd.QuestionGivenTime.TryAdd(p2Id, DateTime.MinValue);
                qd.TimeTaken.TryAdd(p1Id, 0);
                qd.TimeTaken.TryAdd(p2Id, 0);
                qd.IsCorrect.TryAdd(p1Id, false);
                qd.IsCorrect.TryAdd(p2Id, false);

                state.AttemptedQuestionsDetails.Add(qd);
            }

            return state;
        }

        [Fact]
        public async Task CreateBattleAsync_ShouldInitializeBattleState()
        {
            // Arrange
            var service = CreateService();
            var dto = new MatchmakingResultDTO
            {
                Player = new MatchmakingPlayerDTO
                {
                    UserId = 11,
                    ConnectionId = "connA",
                    BattleId = 99
                },
                Opponent = new MatchmakingPlayerDTO
                {
                    UserId = 22,
                    ConnectionId = "connB",
                    BattleId = 99
                }
            };

            _quizMapRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuizToBaseQuestionMap, bool>>>()))
                        .ReturnsAsync(5);

            // Act
            var state = await service.CreateBattleAsync(dto, battleId: 99, CancellationToken.None);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(11, state.Player1Id);
            Assert.Equal(22, state.Player2Id);
            Assert.Equal(5, state.TotalQuestions);
            Assert.True(BattleStateManager.TryGetBattle(state.BattleAttemptId, out var _));

            // cleanup
            BattleStateManager.RemoveBattle(state.BattleAttemptId);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldReturnBattleNotFound_WhenBattleMissing()
        {
            var service = CreateService();

            var res = await service.SubmitAnswerAsync(12345, "conn", 1, "A", userId: 1);
            Assert.NotNull(res);
            Assert.Equal(Constants.BATTLE_NOT_FOUND, res.ErrorMessage);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldValidateInvalidQuestionIndex()
        {
            var service = CreateService();
            var state = CreateSimpleState(1000, totalQuestions: 2);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", questionIndex: 0, answer: "x", userId: state.Player1Id);
            Assert.Equal(Constants.INVALID_QUESTION_INDEX, res.ErrorMessage);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldReturnAlreadyAnswered_IfCurrentIndexGreater()
        {
            var service = CreateService();
            var state = CreateSimpleState(2000);
            // set current index past
            state.CurrentIndex[state.Player1Id] = 3;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", questionIndex: 2, answer: "x", userId: state.Player1Id);
            Assert.Equal(Constants.ALREADY_ANSWERED, res.ErrorMessage);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldReturnAnswerCurrentQuestionOnly_IfNotMatching()
        {
            var service = CreateService();
            var state = CreateSimpleState(3000);
            // current index stays 1; try to answer question 2
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", questionIndex: 2, answer: "x", userId: state.Player1Id);
            Assert.Equal(Constants.ANSWER_CURRENT_QUESTION_ONLY, res.ErrorMessage);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldReturnQuestionNotFound_IfStateMissingQuestionDetail()
        {
            var service = CreateService();
            var state = CreateSimpleState(4000, totalQuestions: 1);
            // intentionally clear attempted details
            state.AttemptedQuestionsDetails.Clear();
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", questionIndex: 1, answer: "x", userId: state.Player1Id);
            Assert.Equal(Constants.QUESTION_NOT_FOUND, res.ErrorMessage);
        }

        [Fact]
        public async Task HandleTimeoutAsync_ReturnsNull_WhenBattleNotFound()
        {
            var service = CreateService();
            var result = await service.HandleTimeoutAsync(9999, "conn", 1, userId: 1);
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleTimeoutAsync_ReturnsNull_WhenIndexMismatch()
        {
            var service = CreateService();
            var state = CreateSimpleState(6000);
            // set current index to 2 but timeout called for 1 => mismatch
            state.CurrentIndex[state.Player1Id] = 2;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var res = await service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id);
            Assert.Null(res);
        }

        [Fact]
        public async Task FinalizeBattleAsync_DecidesWinner_ByScore_OrByTime_OrDraw()
        {
            var service = CreateService();

            // Setup common BattleStatus repo behavior (GetAsync returns an object)
            _battleStatusRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<BattleStatus, bool>>>(),
                    It.IsAny<Func<IQueryable<BattleStatus>, IQueryable<BattleStatus>>>()))
                .ReturnsAsync(new BattleStatus { Id = 999 });


            // Case 1: Player1 higher score
            var s1 = CreateSimpleState(9001);
            s1.Score[s1.Player1Id] = 50;
            s1.Score[s1.Player2Id] = 30;
            // mark some correct answers so counts populate
            s1.AttemptedQuestionsDetails[0].IsCorrect[s1.Player1Id] = true;
            s1.AttemptedQuestionsDetails[0].TimeTaken[s1.Player1Id] = 5;
            s1.AttemptedQuestionsDetails[0].IsCorrect[s1.Player2Id] = false;
            s1.AttemptedQuestionsDetails[0].TimeTaken[s1.Player2Id] = 10;
            BattleStateManager.AddBattle(s1.BattleAttemptId, s1);

            var r1 = await service.FinalizeBattleAsync(s1, CancellationToken.None);
            Assert.Equal(s1.Player1Id, r1.WinnerId);
            // ensure repository AddAsync called
            _battleResultRepo.Verify(x => x.AddAsync(It.IsAny<BattleResult>()), Times.Once);

            // Case 2: tie on score, player2 faster -> player2 wins
            var s2 = CreateSimpleState(9002);
            s2.Score[s2.Player1Id] = 40;
            s2.Score[s2.Player2Id] = 40;
            // set times so player2 is faster
            s2.AttemptedQuestionsDetails[0].TimeTaken[s2.Player1Id] = 40;
            s2.AttemptedQuestionsDetails[0].TimeTaken[s2.Player2Id] = 20;
            BattleStateManager.AddBattle(s2.BattleAttemptId, s2);

            var r2 = await service.FinalizeBattleAsync(s2, CancellationToken.None);
            Assert.Equal(s2.Player2Id, r2.WinnerId);

            // Case 3: tie on score and time => draw
            var s3 = CreateSimpleState(9003);
            s3.Score[s3.Player1Id] = 10;
            s3.Score[s3.Player2Id] = 10;
            s3.AttemptedQuestionsDetails[0].TimeTaken[s3.Player1Id] = 15;
            s3.AttemptedQuestionsDetails[0].TimeTaken[s3.Player2Id] = 15;
            BattleStateManager.AddBattle(s3.BattleAttemptId, s3);

            var r3 = await service.FinalizeBattleAsync(s3, CancellationToken.None);
            Assert.Null(r3.WinnerId);
        }

        [Fact]
        public async Task IntruptByPlayer_OnlyOnePlayerInterrupts_ReturnsNullAndMarksCompleted()
        {
            var service = CreateService();
            var state = CreateSimpleState(8080);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var result = await service.IntruptByPlayer(state.BattleAttemptId, state.Player1Id);
            // When only one player interrupted, returns null (no finished)
            Assert.Null(result);
            Assert.True(state.Completed[state.Player1Id]);
        }

        [Fact]
        public async Task IntruptByPlayer_BothPlayersCompleted_ShouldReturnFinishedDto()
        {
            var service = CreateService();
            var state = CreateSimpleState(8081);
            // mark both completed before call
            state.Completed[state.Player1Id] = true;
            state.Completed[state.Player2Id] = true;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            // ensure FinalizeBattleAsync will run and return a BattleResult
            _battleStatusRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<BattleStatus, bool>>>(),
                    It.IsAny<Func<IQueryable<BattleStatus>, IQueryable<BattleStatus>>>()))
                .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });


            var finished = await service.IntruptByPlayer(state.BattleAttemptId, state.Player1Id);
            Assert.NotNull(finished);
            Assert.Equal(state.BattleAttemptId, finished.BattleId);
        }

        [Fact]
        public async Task GetQuestionForPlayerAsync_ReturnsNull_WhenIndexOutOfBounds()
        {
            var service = CreateService();
            var state = CreateSimpleState(7070, totalQuestions: 2);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var dto1 = await service.GetQuestionForPlayerAsync(state, "c", state.Player1Id, 0);
            Assert.Null(dto1);

            var dto2 = await service.GetQuestionForPlayerAsync(state, "c", state.Player1Id, 3);
            Assert.Null(dto2);
        }

        [Fact]
        public async Task ValidateAnswer_UsesAI_ForLongAnswerTypes_And_MCQ_ForObjective()
        {
            var service = CreateService();

            // Prepare a long-answer type question (QueTypeId 3)
            var longQ = new BaseQuestion
            {
                Id = 2001,
                QueTypeId = 3,
                QueText = "Essay",
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                    {
                        new QuestionOptionsAnswer { Key = "x", Value = "dummy", IsDeleted = false }
                    }

            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(longQ);

            // AI returns TRUE
            _aiMock.Setup(a => a.GetResponseAsync(It.IsAny<string>())).ReturnsAsync("TRUE some explanation");

            // Invoke ValidateAnswer indirectly through SubmitAnswer path: create a state and put question id 2001
            var state = CreateSimpleState(7777, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionId = 2001;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "cX", questionIndex: 1, answer: "some long answer", userId: state.Player1Id);
            // Because AI returned TRUE, IsCorrect should be true and XP updated
            Assert.NotNull(res);
            Assert.NotNull(res.LastAnswerdQuestionDetail);
            Assert.True(res.LastAnswerdQuestionDetail.IsCorrect || state.Score[state.Player1Id] > 0);
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesNullAnswer()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5000, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1, // Objective/MCQ
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, null, state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.False(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(0, state.Score[state.Player1Id]);
        }

        [Fact]
        public async Task HandleTimeoutAsync_HandlesMissingQuestionDetail()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6001, totalQuestions: 1);
            state.AttemptedQuestionsDetails.Clear(); // Simulate missing question details
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            // Act
            var res = await service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.False(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(state.TotalQuestions + 1, state.CurrentIndex[state.Player1Id]);
            Assert.True(state.Completed[state.Player1Id]);
        }

        [Fact]
        public async Task FinalizeBattleAsync_HandlesEmptyAttemptedQuestions()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(9004);
            state.AttemptedQuestionsDetails.Clear(); // Empty question details
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });

            // Act
            var result = await service.FinalizeBattleAsync(state, CancellationToken.None);

            // Assert
            Assert.Null(result.WinnerId); // Draw due to no questions
            Assert.Equal(0, result.User1CorrectedAns);
            Assert.Equal(0, result.User2CorrectedAns);
            Assert.Equal(TimeSpan.Zero, result.User1TakenTime);
            Assert.Equal(TimeSpan.Zero, result.User2TakenTime);
        }

        [Fact]
        public async Task GetBattleInstructions_ThrowsAppException_WhenBattleNotFound()
        {
            // Arrange
            var service = CreateService();
            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), It.IsAny<Func<IQueryable<BattleStatus>, IQueryable<BattleStatus>>>()))
                             .ReturnsAsync((BattleStatus)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => service.GetBattleInstructions(999));
        }

        [Fact]
        public async Task ValidateAnswer_HandlesMCQWithNoCorrectAnswer()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(7778, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionId = 2002;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 2002,
                QueTypeId = 1, // Objective/MCQ
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = "not_answer", Value = "B", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "B", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.False(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal("", res.LastAnswerdQuestionDetail.CorrectAnswer);
        }

        [Fact]
        public async Task GetQuestionForPlayerAsync_HandlesExistingQuestionDetails()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(7004, totalQuestions: 2);
            // Pre-initialize question details
            state.AttemptedQuestionsDetails[0].QuestionId = 101;
            state.AttemptedQuestionsDetails[0].Index = 1;
            state.AttemptedQuestionsDetails[0].Xp = 10;
            state.AttemptedQuestionsDetails[0].TimeLimit = 30;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });

            var rawQuestion = new RawBattleQuestionDto
            {
                QuizQuestionId = 101,
                Options = "[{\"Id\": 1, \"Value\": \"A\"}]",
                Xp = 15, // Different XP to verify it doesn't overwrite
                Time = 45 // Different time to verify it doesn't overwrite
            };
            _sqlRepo.Setup(r => r.SqlQuerySingleAsync<RawBattleQuestionDto>(It.IsAny<string>()))
                               .ReturnsAsync(rawQuestion);

            var mappedDto = new BattleQuestionResponseDto { QuestionIndex = 1 };
            _mapper.Setup(m => m.Map<BattleQuestionResponseDto>(It.IsAny<RawBattleQuestionDto>()))
                   .Returns(mappedDto);

            // Act
            var result = await service.GetQuestionForPlayerAsync(state, "c1", state.Player1Id, 1, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Options);
            Assert.Equal(1, result.QuestionIndex);
            Assert.Equal(101, state.AttemptedQuestionsDetails[0].QuestionId); // Unchanged
            Assert.Equal(10, state.AttemptedQuestionsDetails[0].Xp); // Unchanged
            Assert.Equal(30, state.AttemptedQuestionsDetails[0].TimeLimit); // Unchanged
            Assert.Equal(0, state.AttemptedQuestionsDetails[0].TimeTaken[state.Player1Id]);
            Assert.True(state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] > DateTime.MinValue);
        }

        [Fact]
        public async Task GetQuestionForPlayerAsync_ThrowsException_WhenSqlQueryFails()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(7005, totalQuestions: 2);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });

            _sqlRepo.Setup(r => r.SqlQuerySingleAsync<RawBattleQuestionDto>(It.IsAny<string>()))
                               .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.GetQuestionForPlayerAsync(state, "c1", state.Player1Id, 1, CancellationToken.None));
        }

        [Fact]
        public async Task CreateBattleAsync_HandlesZeroTotalQuestions()
        {
            // Arrange
            var service = CreateService();
            var dto = new MatchmakingResultDTO
            {
                Player = new MatchmakingPlayerDTO { UserId = 11, ConnectionId = "connA", BattleId = 99 },
                Opponent = new MatchmakingPlayerDTO { UserId = 22, ConnectionId = "connB", BattleId = 99 }
            };
            _quizMapRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<QuizToBaseQuestionMap, bool>>>()))
                        .ReturnsAsync(0);

            // Act
            var state = await service.CreateBattleAsync(dto, 99, CancellationToken.None);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(0, state.TotalQuestions);
            Assert.True(BattleStateManager.TryGetBattle(state.BattleAttemptId, out var _));
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesNoTimer()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5001, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            state.ActiveTimers.Clear(); // No timer for user
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.True(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(10, state.Score[state.Player1Id]); // XP from CreateSimpleState
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesNoQuestionGivenTime()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5002, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionGivenTime.Clear(); // No timing data
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.True(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(0, state.AttemptedQuestionsDetails[0].TimeTaken[state.Player1Id]);
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesValidateAnswerException()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5003, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id));
        }

        [Fact]
        public async Task HandleTimeoutAsync_HandlesNoTimer()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6002, totalQuestions: 1);
            state.ActiveTimers.Clear(); // No timer for user
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.False(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(state.TotalQuestions + 1, state.CurrentIndex[state.Player1Id]);
            Assert.True(state.Completed[state.Player1Id]);
        }

        [Fact]
        public async Task FinalizeBattleAsync_HandlesNullBattleStatus()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(9005);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync((BattleStatus)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                service.FinalizeBattleAsync(state, CancellationToken.None));
        }

        [Fact]
        public async Task IntruptByPlayer_HandlesNegativeQuestionDetailIndex()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(8083);
            state.CurrentIndex[state.Player1Id] = 0; // Force negative index
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            // Act
            var result = await service.IntruptByPlayer(state.BattleAttemptId, state.Player1Id);

            // Assert
            Assert.Null(result);
            Assert.True(state.Completed[state.Player1Id]);
            Assert.Equal(state.TotalQuestions + 1, state.CurrentIndex[state.Player1Id]);
        }

        [Fact]
        public async Task ValidateAnswer_HandlesNullQuestion()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(7779, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionId = 2003;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync((BaseQuestion)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id));
        }

        [Fact]
        public async Task CheckAnswer_HandlesInvalidAIResponse()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(7780, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionId = 2004;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 2004,
                QueTypeId = 3,
                QueText = "Essay",
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = "x", Value = "dummy", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _aiMock.Setup(a => a.GetResponseAsync(It.IsAny<string>())).ReturnsAsync("INVALID response");

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "some answer", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.False(res.LastAnswerdQuestionDetail.IsCorrect); // Invalid response treated as FALSE
        }

        [Fact]
        public async Task GetQuestionForPlayerAsync_HandlesBattleStatusRepoException()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(7007, totalQuestions: 2);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.GetQuestionForPlayerAsync(state, "c1", state.Player1Id, 1, CancellationToken.None));
        }

        [Fact]
        public async Task SubmitAnswerAsync_FetchesNextQuestion_WhenNextIndexValid()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5004, totalQuestions: 2); // 2 questions to allow next question
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            var nextQuestion = new RawBattleQuestionDto
            {
                QuizQuestionId = 101,
                Options = "[{\"Id\": 2, \"Value\": \"B\"}]",
                Xp = 10,
                Time = 30
            };
            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });
            _sqlRepo.Setup(r => r.SqlQuerySingleAsync<RawBattleQuestionDto>(It.IsAny<string>()))
                    .ReturnsAsync(nextQuestion);
            _mapper.Setup(m => m.Map<BattleQuestionResponseDto>(It.IsAny<RawBattleQuestionDto>()))
                   .Returns(new BattleQuestionResponseDto { QuestionIndex = 2 });

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.NextQuestion);
            Assert.Equal("c1", res.NextQuestion.ConnectionId);
            Assert.Equal(2, res.NextQuestion.Question.QuestionIndex);
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesGetQuestionException()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5006, totalQuestions: 2);
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id));
        }

        [Fact]
        public async Task SubmitAnswerAsync_FinalizesBattle_WhenBothPlayersCompleted()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5007, totalQuestions: 1);
            state.Completed[state.Player2Id] = true; // Player 2 already completed
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });
            _battleResultRepo.Setup(r => r.AddAsync(It.IsAny<BattleResult>()))
                             .Returns(Task.CompletedTask);

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.Finished);
            Assert.Equal(state.BattleAttemptId, res.Finished.BattleId);
            _battleResultRepo.Verify(r => r.AddAsync(It.IsAny<BattleResult>()), Times.Once());
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesFinalizeBattleException()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5008, totalQuestions: 1);
            state.Completed[state.Player2Id] = true;
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id));
        }

        [Fact]
        public async Task HandleTimeoutAsync_FetchesNextQuestion_WhenNextIndexValid()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6003, totalQuestions: 2);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            var nextQuestion = new RawBattleQuestionDto
            {
                QuizQuestionId = 101,
                Options = "[{\"Id\": 2, \"Value\": \"B\"}]",
                Xp = 10,
                Time = 30
            };
            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });
            _sqlRepo.Setup(r => r.SqlQuerySingleAsync<RawBattleQuestionDto>(It.IsAny<string>()))
                    .ReturnsAsync(nextQuestion);
            _mapper.Setup(m => m.Map<BattleQuestionResponseDto>(It.IsAny<RawBattleQuestionDto>()))
                   .Returns(new BattleQuestionResponseDto { QuestionIndex = 2 });

            // Act
            var res = await service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.NextQuestion);
            Assert.Equal("c1", res.NextQuestion.ConnectionId);
            Assert.Equal(2, res.NextQuestion.Question.QuestionIndex);
        }

        [Fact]
        public async Task HandleTimeoutAsync_HandlesGetQuestionException()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6005, totalQuestions: 2);
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id));
        }

        [Fact]
        public async Task HandleTimeoutAsync_FinalizesBattle_WhenBothPlayersCompleted()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6006, totalQuestions: 1);
            state.Completed[state.Player2Id] = true; // Player 2 already completed
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });
            _battleResultRepo.Setup(r => r.AddAsync(It.IsAny<BattleResult>()))
                             .Returns(Task.CompletedTask);

            // Act
            var res = await service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.Finished);
            Assert.Equal(state.BattleAttemptId, res.Finished.BattleId);
            _battleResultRepo.Verify(r => r.AddAsync(It.IsAny<BattleResult>()), Times.Once());
        }

        [Fact]
        public async Task HandleTimeoutAsync_HandlesFinalizeBattleException()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6007, totalQuestions: 1);
            state.Completed[state.Player2Id] = true;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id));
        }

        [Fact]
        public async Task FinalizeBattleAsync_HandlesEmptyActiveTimers()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(9007);
            state.ActiveTimers.Clear(); // No timers to cancel
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });
            _battleResultRepo.Setup(r => r.AddAsync(It.IsAny<BattleResult>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await service.FinalizeBattleAsync(state, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(state.ActiveTimers.IsEmpty);
            _battleResultRepo.Verify(r => r.AddAsync(It.IsAny<BattleResult>()), Times.Once());
        }

        [Fact]
        public async Task SubmitAnswerAsync_HandlesTimerCancellation()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(5011, totalQuestions: 1);
            state.AttemptedQuestionsDetails[0].QuestionGivenTime[state.Player1Id] = DateTime.UtcNow.AddSeconds(-5);
            var cts = new CancellationTokenSource();
            state.ActiveTimers[state.Player1Id] = cts;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.SubmitAnswerAsync(state.BattleAttemptId, "c1", 1, "A", state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.True(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(10, state.Score[state.Player1Id]);
            Assert.True(state.ActiveTimers.IsEmpty); // Timer removed and disposed
            Assert.Throws<ObjectDisposedException>(() => cts.Token); // Verify CTS was disposed
        }

        [Fact]
        public async Task HandleTimeoutAsync_HandlesTimerCancellation()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(6009, totalQuestions: 1);
            var cts = new CancellationTokenSource();
            state.ActiveTimers[state.Player1Id] = cts;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            var question = new BaseQuestion
            {
                Id = 100,
                QueTypeId = 1,
                QuestionOptionsAnswers = new List<QuestionOptionsAnswer>
                {
                    new QuestionOptionsAnswer { Key = Constants.QUESTION_KEY_ANSWER, Value = "A", IsDeleted = false }
                }
            };
            _baseQRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>(), It.IsAny<Func<IQueryable<BaseQuestion>, IQueryable<BaseQuestion>>>()))
                      .ReturnsAsync(question);

            // Act
            var res = await service.HandleTimeoutAsync(state.BattleAttemptId, "c1", 1, state.Player1Id);

            // Assert
            Assert.NotNull(res);
            Assert.False(res.LastAnswerdQuestionDetail.IsCorrect);
            Assert.Equal(state.TotalQuestions + 1, state.CurrentIndex[state.Player1Id]);
            Assert.True(state.Completed[state.Player1Id]);
            Assert.True(state.ActiveTimers.IsEmpty); // Timer removed and disposed
            Assert.Throws<ObjectDisposedException>(() => cts.Token); // Verify CTS was disposed
        }

        [Fact]
        public async Task FinalizeBattleAsync_HandlesMultipleTimers()
        {
            // Arrange
            var service = CreateService();
            var state = CreateSimpleState(9008);
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            state.ActiveTimers[state.Player1Id] = cts1;
            state.ActiveTimers[state.Player2Id] = cts2;
            BattleStateManager.AddBattle(state.BattleAttemptId, state);

            _battleStatusRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleStatus, bool>>>(), null))
                             .ReturnsAsync(new BattleStatus { Id = state.BattleAttemptId });
            _battleResultRepo.Setup(r => r.AddAsync(It.IsAny<BattleResult>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await service.FinalizeBattleAsync(state, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(state.ActiveTimers.IsEmpty); // All timers cleared
            Assert.Throws<ObjectDisposedException>(() => cts1.Token); // Verify cts1 disposed
            Assert.Throws<ObjectDisposedException>(() => cts2.Token); // Verify cts2 disposed
            _battleResultRepo.Verify(r => r.AddAsync(It.IsAny<BattleResult>()), Times.Once());
        }
    }

    public class QuestionOptionAnswer
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

}
