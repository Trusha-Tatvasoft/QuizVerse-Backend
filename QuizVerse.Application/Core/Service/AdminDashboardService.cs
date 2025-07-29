using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class AdminDashboardService(ISqlQueryRepository _sqlQueryRepository) : IAdminDashboardService
{
    public async Task<AdminDashboardResponse> GetStatisticsData()
    {
        RawAdminDashboardMetricsDTO raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawAdminDashboardMetricsDTO>(SqlConstants.GET_DASHBOARD_METRICS);

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

        string query = string.Format(SqlConstants.CHART_FUNCTION_CALL_TEMPLATE, functionName);

        return await _sqlQueryRepository.SqlQueryListAsync<ChartDataDTO>(query, param1, param2);
    }

    public async Task<List<ChartDataDTO>> GetUserEngagementData(string startDate, string endDate)
    {
        return await GetChartDataAsync(SqlConstants.GET_USER_ENGAGEMENT_CHART_DATA, startDate, endDate);
    }

    public async Task<List<ChartDataDTO>> GetRevenueTrendData(string startDate, string endDate)
    {
        return await GetChartDataAsync(SqlConstants.GET_REVENUE_TREND_CHART_DATA, startDate, endDate);
    }

    public async Task<List<ChartDataDTO>> GetPerformanceScoreData(string startDate, string endDate)
    {
        return await GetChartDataAsync(SqlConstants.GET_PERFORMANCE_SCORE_CHART_DATA, startDate, endDate);
    }
}
