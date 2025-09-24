using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserBattlesService
{
    Task<List<UserAvailableBattleDto>> GetUserAvailableBattles();
    Task<List<UserRecentBattleDto>> GetUserRecentBattles();
    Task<List<UserBattleLeaderboardData>> GetBattleLeaderboardList();
    Task<string> SendBattleRequest(SendBattleRequestDTO dto);
    Task<UserBattleResult> GetBattleResult(int battleId);
}
