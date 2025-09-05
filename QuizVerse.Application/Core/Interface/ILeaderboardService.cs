using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface ILeaderboardService
{
    public Task<UserPerformanceResponseDto> GetUserLeaderboardStats();
    public Task<List<LeaderboardGlobalRankingResponseDto>> GetLeaderboardGlobalRanking();
    public Task<List<WeeklyLeaderBoardResponseDto>> GetWeeklyLeaderboardRanking();
    public Task<List<CategoryWiseLeaderBoardResponseDto>> GetQuizCategoryWiseLeaderboardRanking(int categoryId);
    public Task<List<MonthlyChampionsResponseDto>> GetMonthlyChampions(int month, int year);
}
