using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserDashboardService
{
    Task<UserDashboardResponse> GetStatisticsData();

    Task<List<RecentQuizResponse>> GetRecentQuizzes(bool viewAll);

    Task<FeaturedQuizListDTO> GetFeaturedQuizzes(int batchNumber = 1);

    Task<List<BattleRequestDTO>> GetBattleRequests();

    Task<bool> UpdateBattleRequestStatus(BattleRequestActionDTO dto);
    
    Task<RankProgressDTO> GetRankProgressAsync();
}
