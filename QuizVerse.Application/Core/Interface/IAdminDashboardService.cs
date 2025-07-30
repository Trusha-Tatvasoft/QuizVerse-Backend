using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetStatisticsData();

    Task<List<ChartDataDTO>> GetUserEngagementData(string startDate, string endDate);

    Task<List<ChartDataDTO>> GetRevenueTrendData(string startDate, string endDate);

    Task<List<ChartDataDTO>> GetPerformanceScoreData(string startDate, string endDate);
}
