using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface ILeaderboardService
{
    public Task<UserPerformanceResponseDto> GetUserLeaderboardStats();
    public Task<List<LeaderboardGlobalRankingResponseDto>> GetLeaderboardGlobalRanking();
}
