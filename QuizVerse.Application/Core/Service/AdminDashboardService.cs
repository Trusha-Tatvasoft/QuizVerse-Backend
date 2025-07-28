using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class AdminDashboardService(IGenericRepository<Admin> adminRepository) : IAdminDashboardService
{
    private readonly IGenericRepository<Admin> _adminRepository = adminRepository;

    public async Task<AdminDashboardResponse> GetDashboardSummaryAsync()
    {
        const string sql = "SELECT * FROM get_admin_dashboard_metrics()";

        RawAdminDashboardMetricsDTO raw = await _adminRepository.SqlQuerySingleAsync<RawAdminDashboardMetricsDTO>(sql);

        return new AdminDashboardResponse
        {
            TotalUsers = new()
            {
                Value = raw.TotalUsers,
                TrendPercentage = (double)raw.UserGrowthPercent,
            },
            ActiveQuizzes = new()
            {
                Value = raw.ActiveQuizzes,
                TrendPercentage = (double)raw.QuizGrowthPercent,
            },
            Revenue = new()
            {
                Value = raw.TotalRevenue,
                TrendPercentage = (double)raw.RevenueGrowthPercent,
            },
            Reports = new()
            {
                Value = raw.TotalReports,
                TrendPercentage = (double)raw.ReportGrowthPercent,
            }
        };
    }

    private static DateTime ToDate(string dateString)
    {
        return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
    }

    private async Task<List<ChartDataDTO>> GetChartDataAsync(string functionName, string startDate, string endDate)
    {
        DateTime start = ToDate(startDate);
        DateTime end = ToDate(endDate);

        NpgsqlParameter param1 = new("start", NpgsqlDbType.Date) { Value = start };
        NpgsqlParameter param2 = new("end", NpgsqlDbType.Date) { Value = end };

        string query = $"SELECT * FROM {functionName}({{0}}, {{1}})";

        return await _adminRepository.SqlQueryListAsync<ChartDataDTO>(query, param1, param2);
    }

    public async Task<List<ChartDataDTO>> GetUserEngagementChartData(string startDate, string endDate)
    {
        return await GetChartDataAsync("get_user_engagement_chart_data", startDate, endDate);
    }

    public async Task<List<ChartDataDTO>> GetRevenueTrendChartData(string startDate, string endDate)
    {
        return await GetChartDataAsync("get_revenue_trend_chart_data", startDate, endDate);
    }

    public async Task<List<ChartDataDTO>> GetPerformanceScoreChartData(string startDate, string endDate)
    {
        return await GetChartDataAsync("get_performance_score_chart_data", startDate, endDate);
    }
}
