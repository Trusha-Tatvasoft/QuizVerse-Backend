using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserBattlesService
{
    Task<List<UserRecentBattleDto>> GetUserRecentBattles();
    Task<List<UserBattleLeaderboardData>> GetBattleLeaderboardList();
}
