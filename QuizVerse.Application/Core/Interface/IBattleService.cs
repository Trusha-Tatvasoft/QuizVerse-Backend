using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IBattleService
{
    bool TryGetBattle(int battleId, out BattleState? state);

    Task<BattleState> CreateBattleAsync(MatchmakingResultDTO dto, int battleId, CancellationToken ct = default);

    Task<SubmitAnswerResult> SubmitAnswerAsync(
        int battleId,
        string connectionId,
        int questionIndex,
        string answer,
        int userId,
        CancellationToken ct = default);

    Task<TimeoutResult?> HandleTimeoutAsync(
        int battleId,
        string connectionId,
        int questionIndex,
        int userId,
        CancellationToken ct = default);

    Task<BattleQuestionResponseDto?> GetQuestionForPlayerAsync(
        BattleState state,
        string connectionId,
        int userId,
        int questionIndex,
        CancellationToken ct = default);

    Task<BattleInstructionDTO> GetBattleInstructions(int battleAttemptId);

    Task<BattleResult> FinalizeBattleAsync(BattleState state, CancellationToken ct);
    
    Task<BattleFinishedDto?> IntruptByPlayer(int attemptId, int userId, CancellationToken ct = default);
}
