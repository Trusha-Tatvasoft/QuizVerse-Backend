using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserBattlesService
{
    Task<List<UserAvailableBattleDto>> GetUserAvailableBattles();
    Task<UserBattleHistoryResponseDto> GetUserBattleHistory(UserBattleHistoryRequestDto dto);
    Task<List<UserBattleLeaderboardData>> GetBattleLeaderboardList();
    Task<string> SendBattleRequest(SendBattleRequestDTO dto);
    Task<UserBattleResult> GetBattleResult(int battleId);
    Task<bool> CheckUserExistence(string userName);
    Task<List<SearchUserResponseDto>> SearchUsersAsync(string userName, int battleId);
}
