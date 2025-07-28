using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetDashboardSummaryAsync();

    Task<List<ChartDataDTO>> GetUserEngagementChartData(string startDate, string endDate);

    Task<List<ChartDataDTO>> GetRevenueTrendChartData(string startDate, string endDate);

    Task<List<ChartDataDTO>> GetPerformanceScoreChartData(string startDate, string endDate);
}
