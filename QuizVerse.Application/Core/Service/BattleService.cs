using System.Collections.Concurrent;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

// Create a static battle state manager
public static class BattleStateManager
{
    private static readonly ConcurrentDictionary<int, BattleState> _battles = new();

    public static bool TryGetBattle(int battleId, out BattleState? state)
        => _battles.TryGetValue(battleId, out state);

    public static void AddBattle(int battleId, BattleState state)
        => _battles[battleId] = state;

    public static bool RemoveBattle(int battleId)
        => _battles.TryRemove(battleId, out _);
    public static IEnumerable<BattleState> GetAllBattles()
        => _battles.Values;
}

// Update BattleService to use the shared state
public class BattleService(
    IAiService _aiService,
    IGenericRepository<BaseQuestion> _baseQuestionRepo,
    IGenericRepository<BattleResult> _battleResultRepo,
    IGenericRepository<BattleStatus> _battleStatusRepo,
    IGenericRepository<BattleList> _battleListRepo,
    IGenericRepository<QuizToBaseQuestionMap> _quizToBaseQuestionMap,
    IMapper _mapper,
    ISqlQueryRepository _sqlQueryRepository
    ) : IBattleService
{
    public bool TryGetBattle(int battleId, out BattleState? state)
        => BattleStateManager.TryGetBattle(battleId, out state);

    public async Task<BattleState> CreateBattleAsync(MatchmakingResultDTO dto, int battleId, CancellationToken ct = default)
    {
        // Create database record for battle status
        var status = new BattleStatus
        {
            BattleId = battleId,
            User1Id = dto.Player!.UserId,
            User2Id = dto.Opponent!.UserId,
            BattleStatus1 = (int)Infrastructure.Enums.BattleStatus.Running
        };
        await _battleStatusRepo.AddAsync(status);

        BattleList battle = await _battleListRepo.GetAsync(b => b.Id == battleId);

        int totalQuestions = await _quizToBaseQuestionMap.CountAsync(q => q.QuizId == battle.QuizId && !q.IsDeleted);

        // Initialize battle state
        BattleState state = new BattleState
        {
            BattleAttemptId = status.Id,
            Player1ConnectionId = dto.Player.ConnectionId,
            Player2ConnectionId = dto.Opponent.ConnectionId,
            TotalQuestions = totalQuestions
        };

        // Set player IDs
        state.Player1Id = dto.Player.UserId;
        state.Player2Id = dto.Opponent.UserId;

        // Initialize player states (both start at question 1 with 0 score)
        state.CurrentIndex[dto.Player.UserId] = 1;
        state.Score[dto.Player.UserId] = 0;
        state.Completed[dto.Player.UserId] = false;

        state.CurrentIndex[dto.Opponent!.UserId] = 1;
        state.Score[dto.Opponent!.UserId] = 0;
        state.Completed[dto.Opponent!.UserId] = false;

        // Use shared state manager
        BattleStateManager.AddBattle(state.BattleAttemptId, state);
        return state;
    }

    public async Task<SubmitAnswerResult> SubmitAnswerAsync(
    int battleId,
    string connectionId,
    int questionIndex,
    string answer,
    int userId,
    CancellationToken ct = default)
    {
        if (!BattleStateManager.TryGetBattle(battleId, out var state))
            return new SubmitAnswerResult { ErrorMessage = Constants.BATTLE_NOT_FOUND };

        //  Validate question index properly
        if (questionIndex < 1 || questionIndex > state.TotalQuestions)
            return new SubmitAnswerResult { ErrorMessage = Constants.INVALID_QUESTION_INDEX };

        //  Get current index, default to 1 if not set
        var currentIndex = state.CurrentIndex.GetValueOrDefault(userId, 1);

        //  Check if already answered this question
        if (currentIndex > questionIndex)
            return new SubmitAnswerResult { ErrorMessage = Constants.ALREADY_ANSWERED };

        if (currentIndex != questionIndex)
            return new SubmitAnswerResult { ErrorMessage = Constants.ANSWER_CURRENT_QUESTION_ONLY };

        //  Ensure the question details exist and use correct index
        var questionDetailIndex = questionIndex - 1;
        if (questionDetailIndex >= state.AttemptedQuestionsDetails.Count)
            return new SubmitAnswerResult { ErrorMessage = Constants.QUESTION_NOT_FOUND };

        var qd = state.AttemptedQuestionsDetails[questionDetailIndex];

        if (qd.QuestionGivenTime.TryGetValue(userId, out var givenAt))
        {
            var elapsed = (int)(DateTime.UtcNow - givenAt).TotalSeconds;
            qd.TimeTaken[userId] = elapsed;
        }

        // Cancel active timer for this user
        if (state.ActiveTimers.TryRemove(userId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }

        (bool correct, string ans) = await ValidateAnswer(qd.QuestionId, answer);
        qd.IsCorrect[userId] = correct;

        if (correct)
        {
            int xp = qd.Xp;
            state.Score.AddOrUpdate(userId, xp, (_, old) => old + xp);
        }

        var p1Score = state.Score.GetValueOrDefault(state.Player1Id);
        var p2Score = state.Score.GetValueOrDefault(state.Player2Id);

        //  Update current index after processing the answer
        var nextIndex = questionIndex + 1;
        state.CurrentIndex[userId] = nextIndex;
        state.Completed[userId] = nextIndex > state.TotalQuestions;

        NextQuestionDto? nextQ = null;
        //  Check if there are more questions (nextIndex <= TotalQuestions)
        if (nextIndex <= state.TotalQuestions)
        {
            var q = await GetQuestionForPlayerAsync(state, connectionId, userId, nextIndex, ct);
            if (q != null)
                nextQ = new NextQuestionDto(connectionId, q);
        }

        BattleFinishedDto? finishedDto = null;
        if (state.Completed.GetValueOrDefault(state.Player1Id) &&
            state.Completed.GetValueOrDefault(state.Player2Id))
        {
            var finished = await FinalizeBattleAsync(state, ct);
            finishedDto = new BattleFinishedDto(battleId, finished);
        }

        int totalCorrectByPlayer1 = state.AttemptedQuestionsDetails
            .Select(q => q.IsCorrect.TryGetValue(state.Player1Id, out var isCorrect) && isCorrect)
            .Count(x => x);

        int totalCorrectByPlayer2 = state.AttemptedQuestionsDetails
            .Select(q => q.IsCorrect.TryGetValue(state.Player2Id, out var isCorrect) && isCorrect)
            .Count(x => x);

        return new SubmitAnswerResult
        {
            ScoreChanged = new ScoreChangedDto(battleId, p1Score, p2Score, state.Player1Id, state.Player2Id, state.CurrentIndex[state.Player1Id], state.CurrentIndex[state.Player2Id], totalCorrectByPlayer1, totalCorrectByPlayer2),
            NextQuestion = nextQ,
            Finished = finishedDto,
            LastAnswerdQuestionDetail = new LastAnswerdQuestionDetail(
                questionIndex,
                correct,
                correct ? qd.Xp : 0,
                ans)
        };
    }

    public async Task<TimeoutResult?> HandleTimeoutAsync(
        int battleId,
        string connectionId,
        int questionIndex,
        int userId,
        CancellationToken ct = default)
    {
        if (!BattleStateManager.TryGetBattle(battleId, out var state))
            return null;

        var currentIndex = state.CurrentIndex.GetValueOrDefault(userId, 1);
        if (currentIndex != questionIndex)
            return null;

        // Cancel and remove the timer
        if (state.ActiveTimers.TryRemove(userId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }

        string ans = null;
        var questionDetailIndex = questionIndex - 1;
        if (questionDetailIndex < state.AttemptedQuestionsDetails.Count)
        {
            var qd = state.AttemptedQuestionsDetails[questionDetailIndex];
            (bool correct, ans) = await ValidateAnswer(qd.QuestionId, "");
            qd.IsCorrect[userId] = false;
            qd.TimeTaken[userId] = qd.TimeLimit;
        }

        // Update to next question
        var nextIndex = questionIndex + 1;
        state.CurrentIndex[userId] = nextIndex;
        state.Completed[userId] = nextIndex > state.TotalQuestions;

        var p1Score = state.Score.GetValueOrDefault(state.Player1Id);
        var p2Score = state.Score.GetValueOrDefault(state.Player2Id);

        NextQuestionDto? nextQuestion = null;
        //  Check if there are more questions
        if (nextIndex <= state.TotalQuestions)
        {
            var q = await GetQuestionForPlayerAsync(state, connectionId, userId, nextIndex, ct);
            if (q != null)
                nextQuestion = new NextQuestionDto(connectionId, q);
        }

        BattleFinishedDto? finished = null;
        if (state.Completed.GetValueOrDefault(state.Player1Id) &&
            state.Completed.GetValueOrDefault(state.Player2Id))
        {
            var res = await FinalizeBattleAsync(state, ct);
            finished = new BattleFinishedDto(battleId, res);
        }

        int totalCorrectByPlayer1 = state.AttemptedQuestionsDetails
            .Select(q => q.IsCorrect.TryGetValue(state.Player1Id, out var isCorrect) && isCorrect)
            .Count(x => x);

        int totalCorrectByPlayer2 = state.AttemptedQuestionsDetails
            .Select(q => q.IsCorrect.TryGetValue(state.Player2Id, out var isCorrect) && isCorrect)
            .Count(x => x);

        return new TimeoutResult
        {
            Scores = new ScoreChangedDto(battleId, p1Score, p2Score, state.Player1Id, state.Player2Id, state.CurrentIndex[state.Player1Id], state.CurrentIndex[state.Player2Id], totalCorrectByPlayer1, totalCorrectByPlayer2),
            NextQuestion = nextQuestion,
            Finished = finished,
            LastAnswerdQuestionDetail = new LastAnswerdQuestionDetail(
                questionIndex,
                false,
                0,
                ans)
        };
    }

    public async Task<BattleResult> FinalizeBattleAsync(BattleState state, CancellationToken ct)
    {
        // Cancel all active timers
        foreach (var kv in state.ActiveTimers.ToList())
        {
            try
            {
                kv.Value.Cancel();
                kv.Value.Dispose();
            }
            catch { }
        }
        state.ActiveTimers.Clear();

        // Remove from state manager
        BattleStateManager.RemoveBattle(state.BattleAttemptId);

        var p1Score = state.Score.GetValueOrDefault(state.Player1Id);
        var p2Score = state.Score.GetValueOrDefault(state.Player2Id);

        int p1Correct = 0, p2Correct = 0;
        int p1TotalSeconds = 0, p2TotalSeconds = 0;

        //  Calculate stats only for questions that were actually attempted
        foreach (var q in state.AttemptedQuestionsDetails)
        {
            // Player 1 stats
            if (q.IsCorrect.TryGetValue(state.Player1Id, out var p1IsCorrect) && p1IsCorrect)
                p1Correct++;

            if (q.TimeTaken.TryGetValue(state.Player1Id, out var p1Time))
                p1TotalSeconds += p1Time;

            // Player 2 stats  
            if (q.IsCorrect.TryGetValue(state.Player2Id, out var p2IsCorrect) && p2IsCorrect)
                p2Correct++;

            if (q.TimeTaken.TryGetValue(state.Player2Id, out var p2Time))
                p2TotalSeconds += p2Time;
        }

        // Determine winner properly
        int? winnerId = null;
        int winnerScore = 0, loserScore = 0;

        if (p1Score > p2Score)
        {
            winnerId = state.Player1Id;
            winnerScore = p1Score;
            loserScore = p2Score;
        }
        else if (p2Score > p1Score)
        {
            winnerId = state.Player2Id;
            winnerScore = p2Score;
            loserScore = p1Score;
        }
        else
        {
            // Scores are equal → check total time
            if (p1TotalSeconds < p2TotalSeconds)
            {
                winnerId = state.Player1Id;
                winnerScore = p1Score;
                loserScore = p2Score;
            }
            else if (p2TotalSeconds < p1TotalSeconds)
            {
                winnerId = state.Player2Id;
                winnerScore = p2Score;
                loserScore = p1Score;
            }
            else
            {
                // Perfect draw (same score, same total time)
                winnerScore = p1Score;
                loserScore = p2Score;
            }
        }

        BattleStatus battleStatus = await _battleStatusRepo.GetAsync(b => b.Id == state.BattleAttemptId);

        //  Set proper battle status
        if (winnerId.HasValue)
            battleStatus.BattleStatus1 = (int)Infrastructure.Enums.BattleStatus.Completed;
        else
            battleStatus.BattleStatus1 = (int)Infrastructure.Enums.BattleStatus.Draw;

        battleStatus.ModifiedDate = DateTime.UtcNow;
        await _battleStatusRepo.UpdateAsync(battleStatus);

        var result = new BattleResult
        {
            BattleStatus = battleStatus.Id,
            WinnerId = winnerId,
            WinnerGainedXp = winnerScore,
            LooserGainedXp = loserScore,
            User1CorrectedAns = p1Correct,
            User2CorrectedAns = p2Correct,
            User1TakenTime = TimeSpan.FromSeconds(p1TotalSeconds),
            User2TakenTime = TimeSpan.FromSeconds(p2TotalSeconds)
        };

        await _battleResultRepo.AddAsync(result);
        return result;
    }

    public async Task<BattleFinishedDto?> IntruptByPlayer(int attemptId, int userId, CancellationToken ct = default)
    {
        if (!BattleStateManager.TryGetBattle(attemptId, out var state))
            return null;

        // Cancel and dispose the active timer for this user
        if (state.ActiveTimers.TryRemove(userId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }

        //  Get the current question index for this user
        var currentQuestionIndex = state.CurrentIndex.GetValueOrDefault(userId, 1);
        var questionDetailIndex = currentQuestionIndex - 1;

        //  Only update timing if there's an active question
        if (questionDetailIndex >= 0 && questionDetailIndex < state.AttemptedQuestionsDetails.Count)
        {
            var questionDetail = state.AttemptedQuestionsDetails[questionDetailIndex];

            //  Only update if this user has timing data for this question
            if (questionDetail.QuestionGivenTime.TryGetValue(userId, out var givenTime))
            {
                var elapsed = (int)(DateTime.UtcNow - givenTime).TotalSeconds;
                questionDetail.TimeTaken[userId] = elapsed;
                // Mark as incorrect since they interrupted
                questionDetail.IsCorrect[userId] = false;
            }
        }

        //  Set completion status properly
        state.CurrentIndex[userId] = state.TotalQuestions + 1;
        state.Completed[userId] = true;

        //  Check if both players have completed (either finished or interrupted)
        var player1Completed = state.Completed.GetValueOrDefault(state.Player1Id);
        var player2Completed = state.Completed.GetValueOrDefault(state.Player2Id);

        BattleFinishedDto? finishedDto = null;
        if (player1Completed && player2Completed)
        {
            // Both players are done - finalize the battle
            var finished = await FinalizeBattleAsync(state, ct);
            finishedDto = new BattleFinishedDto(attemptId, finished);
        }

        return finishedDto;
    }

    public async Task<BattleQuestionResponseDto?> GetQuestionForPlayerAsync(
    BattleState state,
    string connectionId,
    int userId,
    int questionIndex,
    CancellationToken ct = default)
    {
        //  Boundary check should allow questionIndex == state.TotalQuestions
        if (questionIndex < 1 || questionIndex > state.TotalQuestions)
            return null;

        var battleStatus = await _battleStatusRepo
            .GetAsync(b => b.Id == state.BattleAttemptId);

        var raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawBattleQuestionDto>(
            string.Format(SqlConstants.GET_Battle_QUESTIONS_QUERY_TEMPLATE, battleStatus.Id, questionIndex));

        var options = JsonSerializer.Deserialize<List<OptionResponseDto>>(raw.Options ?? "[]") ?? [];

        var dto = _mapper.Map<BattleQuestionResponseDto>(raw);
        dto.Options = options;
        dto.QuestionIndex = questionIndex;

        //  Use questionIndex-1 to access the correct array position
        var questionDetailIndex = questionIndex - 1;

        //  Ensure the list has enough capacity
        while (state.AttemptedQuestionsDetails.Count <= questionDetailIndex)
        {
            state.AttemptedQuestionsDetails.Add(new QuestionsDetail());
        }

        var existing = state.AttemptedQuestionsDetails[questionDetailIndex];

        //  Always initialize the question details properly
        if (existing.QuestionId == 0)
        {
            existing.Index = questionIndex;
            existing.QuestionId = raw.QuizQuestionId;
            existing.Xp = raw.Xp;
            existing.TimeLimit = raw.Time;
        }

        // Set timing for this user
        existing.TimeTaken[userId] = 0;
        existing.QuestionGivenTime[userId] = DateTime.UtcNow;

        return dto;
    }

    private async Task<(bool, string)> ValidateAnswer(int questionId, string? givenAnswer)
    {
        BaseQuestion question = await _baseQuestionRepo.GetAsync(q => q.Id == questionId, includes: q => q.Include(qq => qq.QuestionOptionsAnswers));
        if (question.QueTypeId == 3 || question.QueTypeId == 4)
        {
            QuizAnswerCheckDto quizAnswerCheck = new()
            {
                QuestionName = question.QueText,
                GivenAnswer = givenAnswer,
                CorrectAnswer = string.Join(", ", question.QuestionOptionsAnswers
                                            .Where(o => !o.IsDeleted)
                                            .Select(o => o.Value))
            };

            return await CheckAnswer(quizAnswerCheck);
        }

        // Objective/MCQ
        string correctAnswer = question.QuestionOptionsAnswers
                                .Where(o => !o.IsDeleted &&
                                            o.Key.Equals(Constants.QUESTION_KEY_ANSWER, StringComparison.OrdinalIgnoreCase))
                                .Select(o => o.Value)
                                .FirstOrDefault() ?? "";

        return (string.Equals(givenAnswer?.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase), correctAnswer.Trim());
    }

    private async Task<(bool, string)> CheckAnswer(QuizAnswerCheckDto quizAnswerCheck)
    {
        if (string.IsNullOrWhiteSpace(quizAnswerCheck.GivenAnswer))
            return (false, quizAnswerCheck.CorrectAnswer);

        string prompt = string.Format(
            PromptConstants.CHECK_ANSWER_PROMPT,
            quizAnswerCheck.QuestionName,
            quizAnswerCheck.CorrectAnswer,
            quizAnswerCheck.GivenAnswer
        );

        string response = await _aiService.GetResponseAsync(prompt);

        return (response.Trim().StartsWith("TRUE", StringComparison.OrdinalIgnoreCase), quizAnswerCheck.CorrectAnswer);
    }

    public async Task<BattleInstructionDTO> GetBattleInstructions(int battleAttemptId)
    {
        BattleStatus? battle = await _battleStatusRepo.GetAsync(
            b => b.Id == battleAttemptId && !b.IsDeleted,
            includes: b => b
                .Include(bl => bl.Battle)
                .ThenInclude(q => q.Quiz).ThenInclude(c => c.Category)
                .Include(u1 => u1.Battle).ThenInclude(u2 => u2.BattleQuesDifficultyMaps)
        );
        if (battle == null)
            throw new AppException(Constants.BATTLE_NOT_FOUND);

        return new BattleInstructionDTO
        {
            BattleAttemptId = battleAttemptId,
            BattleName = battle.Battle.Quiz.Name,
            BattleDescription = battle.Battle.Quiz.Description,
            BattleCategory = battle.Battle.Quiz.Category.CategoryName,
            TimeInSeconds = battle.Battle.Quiz.TotalTime,
        };
    }
}