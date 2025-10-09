using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IBattleMatchmakingService
{
    Task<MatchmakingResultDTO> StartMatchmaking(int battleId, int userId, string connectionId);
    Task<MatchmakingResultDTO?> StartFriendBattle(int battleId, int senderUserId, int receiverUserId);
    MatchmakingPlayerDTO? FindOpponent(MatchmakingPlayerDTO player);
    Task<double> GetUserWinRate(int userId);
    Task<PlayerProfileDTO?> GetPlayerProfile(int userId);
    void CancelMatchmaking(int quizId, int userId);
}
