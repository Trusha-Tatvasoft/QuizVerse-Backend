using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using static QuizVerse.Infrastructure.Common.Constants;

namespace QuizVerse.WebAPI.Hubs;

public class BattleHub(
    IBattleMatchmakingService _battleMatchmakingService,
    IBattleService _battleService,
    IServiceScopeFactory _scopeFactory,
    IHubContext<BattleHub> _hubContext,
    IGenericRepository<QuizVerse.Domain.Entities.BattleStatus> _battleStatusRepo,
    IMapper _mapper
) : Hub
{
    public async Task StartMatchmaking(int battleId)
    {
        try
        {
            // Extract user ID from JWT token in SignalR context
            int userId = Context.User?.GetUserId()
                ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

            List<BattleState> stateStored = BattleStateManager.GetAllBattles().Where(s => (s.Player1Id == userId || s.Player2Id == userId)).ToList();

            if (stateStored != null && stateStored.Count() > 0)
            {
                foreach (BattleState item in stateStored)
                {
                    if (item.Connected[userId])
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE);
                        return;
                    }
                }
            }

            // Store battle ID in connection context for cleanup on disconnect
            Context.Items[BATTLE_ID] = battleId;

            // Check if user already has an active battle for this battleId
            var battleResult = await _battleStatusRepo.GetAsync(u => u.BattleId == battleId &&
                (u.User1Id == userId || u.User2Id == userId) && !u.IsDeleted);

            // Handle case where user has an existing running battle
            if (battleResult != null && battleResult.BattleStatus1 == (int)QuizVerse.Infrastructure.Enums.BattleStatus.Running)
            {
                // Check if battle state exists in memory (active battle)
                if (BattleStateManager.TryGetBattle(battleResult.Id, out BattleState stateOld) && stateOld != null)
                {
                    // User disconnected previously - check if they can resume
                    if (stateOld.ConnectionBrokeTime.TryGetValue(userId, out var brokeAt))
                    {
                        var gap = DateTime.UtcNow - brokeAt;

                        // Allow resume within 10 minutes of disconnect
                        if (gap < TimeSpan.FromMinutes(10))
                        {
                            await Clients.Client(Context.ConnectionId)
                                .SendAsync(SignalRMethods.CONTINUE_BATTLE, new
                                {
                                    BattleAttemptId = stateOld.BattleAttemptId,
                                    Message = YOU_HAVE_UNFINISHED_BATTLE
                                });
                            return;
                        }
                        else
                        {
                            // Resume window expired
                            await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, BATTLE_SESSION_EXPIRED);
                            return;
                        }
                    }
                }
                else
                {
                    // Database says running but no state in memory - battle completed
                    await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, BATTLE_ALREADY_COMPLETED);
                    return;
                }
            }

            // Prevent joining already completed battles
            if (battleResult != null && ((battleResult.BattleStatus1 == (int)QuizVerse.Infrastructure.Enums.BattleStatus.Completed) || (battleResult.BattleStatus1 == (int)QuizVerse.Infrastructure.Enums.BattleStatus.Draw)))
            {
                await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, BATTLE_ALREADY_COMPLETED);
                return;
            }

            // Start matchmaking process
            var result = await _battleMatchmakingService.StartMatchmaking(battleId, userId, Context.ConnectionId);

            // Still waiting for opponent
            if (!result.IsMatched)
            {
                await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.SEARCHING);
                return;
            }

            // Match found - notify both players
            await Clients.Client(result.Player!.ConnectionId)
                .SendAsync(SignalRMethods.MATCH_FOUND, result.OpponentProfile);
            await Clients.Client(result.Opponent!.ConnectionId)
                .SendAsync(SignalRMethods.MATCH_FOUND, result.PlayerProfile);

            // Create battle state and save to database
            var state = await _battleService.CreateBattleAsync(result, battleId);
            Context.Items[BATTLE_ATTEMPT_ID_KEY] = state.BattleAttemptId;

            // Validate battle creation was successful
            if (state.BattleAttemptId <= 0)
            {
                await Clients.Client(result.Player.ConnectionId).SendAsync(SignalRMethods.ERROR, FAILED_TO_CREATE_BATTLE);
                await Clients.Client(result.Opponent.ConnectionId).SendAsync(SignalRMethods.ERROR, FAILED_TO_CREATE_BATTLE);
                return;
            }

            // Add both players to SignalR group for battle communication
            await Groups.AddToGroupAsync(result.Player.ConnectionId, state.BattleAttemptId.ToString());
            await Groups.AddToGroupAsync(result.Opponent.ConnectionId, state.BattleAttemptId.ToString());

            // Mark both players as connected in battle state
            state.Connected[state.Player1Id] = true;
            state.Connected[state.Player2Id] = true;

            // Get battle metadata for frontend display
            BattleInstructionDTO battleInstruction = await _battleService.GetBattleInstructions(state.BattleAttemptId);

            // Prepare battle start details for frontend
            BattleStartDetails battleDetails = new BattleStartDetails()
            {
                PlayerProfile = result.PlayerProfile!,
                OpponentProfile = result.OpponentProfile!,
                BattleAttemptId = state.BattleAttemptId,
                TotalQuestions = state.TotalQuestions,
                BattleName = battleInstruction.BattleName
            };

            // Notify both players that battle has started
            await Clients.Group(state.BattleAttemptId.ToString())
                .SendAsync(SignalRMethods.BATTLE_STARTED, battleDetails);

            _ = CountdownToStart(state, state.Player1Id);
            _ = CountdownToStart(state, state.Player2Id);
        }
        catch (Exception ex)
        {
            await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, ex.Message);
        }
    }

    public async Task SubmitAnswer(int battleId, int questionIndex, string answer)
    {
        try
        {
            // Get user ID from context
            int userId = Context.User?.GetUserId()
                ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

            var conn = Context.ConnectionId;

            // Process answer through battle service
            var r = await _battleService.SubmitAnswerAsync(battleId, conn, questionIndex, answer, userId);

            // Handle validation errors (wrong question index, already answered, etc.)
            if (!string.IsNullOrEmpty(r.ErrorMessage))
            {
                await Clients.Client(conn).SendAsync(SignalRMethods.ERROR, r.ErrorMessage);
                return;
            }

            // Send score update to all players in battle
            if (r.ScoreChanged != null)
                await Clients.Group(battleId.ToString()).SendAsync(SignalRMethods.RECEIVE_SCORE_UPDATE, r.ScoreChanged);

            // Send answer result (correct/incorrect, XP gained) to the answering player
            if (r.LastAnswerdQuestionDetail != null)
            {
                await Clients.Client(conn).SendAsync(SignalRMethods.LAST_ANSWERED_DETAIL, r.LastAnswerdQuestionDetail);
            }

            // Send next question if available
            if (r.NextQuestion != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                await Clients.Client(r.NextQuestion.ConnectionId)
                    .SendAsync(SignalRMethods.RECEIVE_QUESTION, r.NextQuestion.Question);

                // Start timer for next question
                var nextIndex = questionIndex + 1;
                var nextCts = new CancellationTokenSource();

                if (BattleStateManager.TryGetBattle(battleId, out var state))
                {
                    state.ActiveTimers[userId] = nextCts;
                    StartTimeout(battleId, r.NextQuestion.ConnectionId, nextIndex, userId, nextCts);
                }
            }

            // Handle battle completion (both players finished)
            if (r.Finished != null)
            {
                await Clients.Group(battleId.ToString())
                    .SendAsync(SignalRMethods.BATTLE_ENDED, _mapper.Map<BattleCompletionResult>(r.Finished.Result));
            }
        }
        catch (Exception ex)
        {
            await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, ex.Message);
        }
    }

    private void StartTimeout(int battleId, string connectionId, int questionIndex, int userId, CancellationTokenSource cts)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Verify battle state still exists
                if (!BattleStateManager.TryGetBattle(battleId, out var state) || state == null)
                    return;

                // Get time limit for this specific question
                var questionDetailIndex = questionIndex - 1;
                if (questionDetailIndex < 0 || questionDetailIndex >= state.AttemptedQuestionsDetails.Count)
                    return;

                var limit = state.AttemptedQuestionsDetails[questionDetailIndex].TimeLimit;

                // Wait for the time limit (can be cancelled if player answers)
                await Task.Delay(TimeSpan.FromSeconds(limit), cts.Token);

                // Create new service scope to avoid disposed context issues
                using var scope = _scopeFactory.CreateScope();
                var battleSvc = scope.ServiceProvider.GetRequiredService<IBattleService>();

                // Process timeout through battle service
                var result = await battleSvc.HandleTimeoutAsync(battleId, connectionId, questionIndex, userId, cts.Token);

                if (result == null) return;

                // Send timeout answer details to the specific player
                if (result.LastAnswerdQuestionDetail != null)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync(SignalRMethods.LAST_ANSWERED_DETAIL, result.LastAnswerdQuestionDetail);
                }

                // Update scores for all players in battle
                await _hubContext.Clients.Group(battleId.ToString()).SendAsync(SignalRMethods.RECEIVE_SCORE_UPDATE, result.Scores);

                // Send next question if available
                if (result.NextQuestion != null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    await _hubContext.Clients.Client(result.NextQuestion.ConnectionId)
                        .SendAsync(SignalRMethods.RECEIVE_QUESTION, result.NextQuestion.Question);

                    // Start timer for next question
                    var nextIndex = questionIndex + 1;
                    var nextCts = new CancellationTokenSource();

                    if (BattleStateManager.TryGetBattle(battleId, out var nextState))
                    {
                        nextState.ActiveTimers[userId] = nextCts;
                        StartTimeout(battleId, result.NextQuestion.ConnectionId, nextIndex, userId, nextCts);
                    }
                }

                // Handle battle completion due to timeout
                if (result.Finished != null)
                    await _hubContext.Clients.Group(battleId.ToString()).SendAsync(SignalRMethods.BATTLE_ENDED, _mapper.Map<BattleCompletionResult>(result.Finished.Result));
            }
            catch (TaskCanceledException)
            {
                // Timer was cancelled because player answered - this is expected
            }
            catch (Exception ex)
            {
                // Log timeout processing errors
                await _hubContext.Clients.Group(battleId.ToString()).SendAsync(SignalRMethods.ERROR, ex.Message);
            }
        });
    }

    public async Task ResumeBattle(int attemptId)
    {
        try
        {
            int userId = Context.User?.GetUserId()
                ?? throw new UnauthorizedAccessException(UNAUTHORIZED_USER);

            // Check if battle state exists in memory
            if (BattleStateManager.TryGetBattle(attemptId, out var state))
            {
                // Prevent duplicate connections
                if (state.Connected[userId])
                {
                    await Clients.Client(Context.ConnectionId)
                        .SendAsync(SignalRMethods.ERROR, YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE);
                    return;
                }

                // Check if resume window is still valid (within 10 minutes)
                if (state.ConnectionBrokeTime[userId] - DateTime.UtcNow < TimeSpan.FromMinutes(10))
                {
                    // Cancel any existing timers for this user
                    try { state.ActiveTimers[userId].Cancel(); state.ActiveTimers[userId].Dispose(); } catch { }

                    // Update connection ID based on player identity
                    if (userId == state.Player1Id)
                    {
                        state.Player1ConnectionId = Context.ConnectionId;
                    }
                    else if (userId == state.Player2Id)
                    {
                        state.Player2ConnectionId = Context.ConnectionId;
                    }
                    else
                    {
                        await Clients.Client(Context.ConnectionId)
                            .SendAsync(SignalRMethods.ERROR, YOU_ARE_NOT_PART_OF_BATTLE);
                        return;
                    }

                    // Get battle instructions for resume display
                    BattleInstructionDTO battleInstruction = await _battleService.GetBattleInstructions(state.BattleAttemptId);

                    // Prepare resume data with current battle state
                    BattleStartDetails battleDetails = new BattleStartDetails()
                    {
                        PlayerProfile = await _battleMatchmakingService.GetPlayerProfile(userId),
                        OpponentProfile = await _battleMatchmakingService.GetPlayerProfile(userId == state.Player1Id ? state.Player2Id : state.Player1Id),
                        BattleAttemptId = state.BattleAttemptId,
                        TotalQuestions = state.TotalQuestions,
                        BattleName = battleInstruction.BattleName
                    };

                    // Send resume confirmation to player
                    await Clients.Client(Context.ConnectionId)
                        .SendAsync(SignalRMethods.BATTLE_RESUMED, battleDetails);

                    // Add player back to battle group
                    await Groups.AddToGroupAsync(Context.ConnectionId, state.BattleAttemptId.ToString());

                    // Mark player as connected
                    state.Connected[userId] = true;

                    // Send current question to resumed player
                    var currentQuestion = await _battleService.GetQuestionForPlayerAsync(state, Context.ConnectionId, userId, state.CurrentIndex[userId]);

                    if (currentQuestion != null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.RECEIVE_QUESTION, currentQuestion);

                        // Restart timer for current question
                        var cts1 = new CancellationTokenSource();
                        state.ActiveTimers[userId] = cts1;
                        // Note: Using hardcoded values here - should use current question index and userId
                        StartTimeout(state.BattleAttemptId, Context.ConnectionId, 1, state.Player1Id, cts1);
                    }
                }
                else
                {
                    // Resume window expired
                    await Clients.Client(Context.ConnectionId)
                        .SendAsync(SignalRMethods.ERROR, RESUME_WINDOW_EXPIRED);
                }
            }
            else
            {
                // Battle state not found in memory
                await Clients.Client(Context.ConnectionId)
                    .SendAsync(SignalRMethods.ERROR, NO_PENDING_BATTLE_TO_RESUME);
            }
        }
        catch (Exception ex)
        {
            await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, ex.Message);
        }
    }

    public async Task IntruptByPlayer(int attemptId)
    {
        try
        {
            int userId = Context.User?.GetUserId()
                ?? throw new UnauthorizedAccessException(UNAUTHORIZED_USER);

            if (!BattleStateManager.TryGetBattle(attemptId, out var state))
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync(SignalRMethods.ERROR, NO_PENDING_BATTLE_TO_INTERRUPT);
                return;
            }

            //  Check if user is part of this battle
            if (userId != state.Player1Id && userId != state.Player2Id)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync(SignalRMethods.ERROR, YOU_ARE_NOT_PART_OF_BATTLE);
                return;
            }

            //  Check if user has already completed/interrupted
            if (state.Completed.GetValueOrDefault(userId))
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync(SignalRMethods.ERROR, YOU_HAVE_ALREADY_COMPLETED_THIS_BATTLE);
                return;
            }

            BattleFinishedDto? finishedDto = await _battleService.IntruptByPlayer(attemptId, userId);

            // Always notify about the interruption first
            await Clients.Group(attemptId.ToString())
                .SendAsync(SignalRMethods.PLAYER_INTERRUPTED, new { UserId = userId });

            if (finishedDto != null)
            {
                // Both players completed - send battle end result
                await Clients.Group(attemptId.ToString())
                    .SendAsync(SignalRMethods.BATTLE_ENDED, _mapper.Map<BattleCompletionResult>(finishedDto.Result));
            }
            else
            {
                // Only one player interrupted - notify about partial interruption
                await Clients.Group(attemptId.ToString())
                    .SendAsync(SignalRMethods.BATTLE_ENDED_FOR_PARTICULAR_PLAYER_DUE_TO_INTERRUPT, new { UserId = userId });

                var p1Score = state.Score.GetValueOrDefault(state.Player1Id);
                var p2Score = state.Score.GetValueOrDefault(state.Player2Id);

                int totalCorrectByPlayer1 = state.AttemptedQuestionsDetails
                    .Count(q => q.IsCorrect.TryGetValue(state.Player1Id, out var isCorrect) && isCorrect);

                int totalCorrectByPlayer2 = state.AttemptedQuestionsDetails
                    .Count(q => q.IsCorrect.TryGetValue(state.Player2Id, out var isCorrect) && isCorrect);

                var scoreUpdate = new ScoreChangedDto(
                    attemptId,
                    p1Score,
                    p2Score,
                    state.Player1Id,
                    state.Player2Id,
                    state.CurrentIndex.GetValueOrDefault(state.Player1Id, 1),
                    state.CurrentIndex.GetValueOrDefault(state.Player2Id, 1),
                    totalCorrectByPlayer1,
                    totalCorrectByPlayer2
                );

                await Clients.Group(attemptId.ToString())
                    .SendAsync(SignalRMethods.RECEIVE_SCORE_UPDATE, scoreUpdate);
            }
        }
        catch (Exception ex)
        {
            await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, ex.Message);
        }
    }

    public Task CancelMatchmaking(int battleId)
    {
        int userId = Context.User?.GetUserId()
            ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

        _battleMatchmakingService.CancelMatchmaking(battleId, userId);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        int? userId = Context.User?.GetUserId();
        if (userId.HasValue && Context.Items.TryGetValue(BATTLE_ID, out var battleIdObj) && battleIdObj is int battleId)
        {
            _battleMatchmakingService.CancelMatchmaking(battleId, userId.Value);
        }

        int? battleAttemptId = Context.Items.TryGetValue(BATTLE_ATTEMPT_ID_KEY, out var attemptObj) && attemptObj is int id ? id : null;
        BattleState state = null;

        if (battleAttemptId.HasValue)
        {
            BattleStateManager.TryGetBattle(battleAttemptId.Value, out state);
        }
        else
        {
            // Fallback: iterate all battles to find this user
            List<BattleState> stateStored = BattleStateManager.GetAllBattles().Where(s => (s.Player1Id == userId || s.Player2Id == userId)).ToList();
            if (stateStored != null && stateStored.Count() > 0)
            {
                foreach (BattleState item in stateStored)
                {
                    if (item.Connected[(int)userId])
                    {
                        state = item;
                        break;
                    }
                }
            }
        }

        if (state == null)
        {
            return Task.CompletedTask;
        }

        if (!state.Connected[userId.Value])
        {
            return Task.CompletedTask;
        }
        else
        {
            state.Connected[userId.Value] = false;
            state.ConnectionBrokeTime[userId.Value] = DateTime.UtcNow;
            if (state.ActiveTimers.TryRemove(userId.Value, out var cts))
            {
                try { cts.Cancel(); } catch { }
                cts.Dispose();
            }
            // Find the last question that has this userId in its QuestionGivenTime
            var last = state.AttemptedQuestionsDetails
                .LastOrDefault(q => q.QuestionGivenTime.ContainsKey(userId.Value));

            if (last != null)
            {
                // Remove the user entry from QuestionGivenTime
                last.QuestionGivenTime.TryRemove(userId.Value, out _);

                // Optionally: if no user entries left in this question, remove the question entirely
                if (last.QuestionGivenTime.IsEmpty)
                {
                    state.AttemptedQuestionsDetails.Remove(last);
                }
            }
            if ((state.Connected.TryGetValue(state.Player1Id, out var stillDisconnectedPlayer1) && !stillDisconnectedPlayer1) && (state.Connected.TryGetValue(state.Player2Id, out var stillDisconnectedPlayer2) && !stillDisconnectedPlayer2))
                _ = HandleBattleCleanupAsync(state);
        }

        return base.OnDisconnectedAsync(exception);
    }

    private async Task HandleBattleCleanupAsync(BattleState state)
    {
        // Wait for potential reconnection (10 minutes)
        await Task.Delay(TimeSpan.FromMinutes(10));

        // Check if both players are still disconnected
        if ((state.Connected.TryGetValue(state.Player1Id, out var stillDisconnectedPlayer1) && !stillDisconnectedPlayer1) &&
            (state.Connected.TryGetValue(state.Player2Id, out var stillDisconnectedPlayer2) && !stillDisconnectedPlayer2))
        {
            // Create new service scope to avoid disposed context issues
            using var scope = _scopeFactory.CreateScope();
            var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();

            // Finalize battle and save results to database
            await battleService.FinalizeBattleAsync(state, new CancellationToken());
        }
    }

    public async Task SkipInstructions(int battleAttemptId)
    {
        int userId = Context.User?.GetUserId() ?? throw new UnauthorizedAccessException();

        if (BattleStateManager.TryGetBattle(battleAttemptId, out var state))
        {
            state.IsSkipInstruction[userId] = true;
        }
        else
        {
            await Clients.Client(Context.ConnectionId).SendAsync(SignalRMethods.ERROR, BATTLE_NOT_FOUND);
        }
    }

    private async Task CountdownToStart(BattleState state, int userId)
    {
        var completed = await Task.WhenAny(WaitForBothPlayersReady(state, userId), Task.Delay(TimeSpan.FromSeconds(58)));

        var connectionId = userId == state.Player1Id ? state.Player1ConnectionId : state.Player2ConnectionId;
        if (state != null)
        {
            state.IsSkipInstruction[userId] = true;
            // Create new service scope to avoid disposed context issues
            using var scope = _scopeFactory.CreateScope();
            var battleSvc = scope.ServiceProvider.GetRequiredService<IBattleService>();

            // Immediately send question to this player
            var question = await battleSvc.GetQuestionForPlayerAsync(
                state,
                connectionId,
                userId,
                1
            );

            if (question != null)
            {
                await _hubContext.Clients.Client(connectionId).SendAsync(SignalRMethods.RECEIVE_QUESTION, question);
                var cts = new CancellationTokenSource();
                state.ActiveTimers[userId] = cts;
                StartTimeout(state.BattleAttemptId, connectionId, 1, userId, cts);
            }
        }
    }

    private async Task WaitForBothPlayersReady(BattleState state, int userId)
    {
        while (true)
        {
            if (state.IsSkipInstruction.TryGetValue(userId, out var p1) &&
                p1)
            {
                break;
            }
            await Task.Delay(500);
        }
    }
}