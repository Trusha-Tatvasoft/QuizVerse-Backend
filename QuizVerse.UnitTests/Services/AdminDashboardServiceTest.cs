using Moq;
using Npgsql;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class AdminDashboardServiceTest
{
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock = new();

    private AdminDashboardService CreateService() =>
        new(_sqlQueryRepoMock.Object);

    [Fact]
    public async Task GetDashboardSummaryAsync_ReturnsMappedData()
    {
        // Arrange
        RawAdminDashboardMetricsDTO rawData = new()
        {
            TotalUsers = 100,
            UserGrowthPercent = 10.5m,
            ActiveQuizzes = 50,
            QuizGrowthPercent = 5.2m,
            TotalRevenue = 1234,
            RevenueGrowthPercent = 12.3m,
            TotalReports = 25,
            ReportGrowthPercent = -3.1m
        };

        _sqlQueryRepoMock
            .Setup(r => r.SqlQuerySingleAsync<RawAdminDashboardMetricsDTO>(SqlConstants.GET_DASHBOARD_METRICS))
            .ReturnsAsync(rawData);

        AdminDashboardService service = CreateService();

        // Act
        AdminDashboardResponse result = await service.GetDashboardSummaryAsync();

        // Assert
        Assert.Equal(100, result.TotalUsers.Value);
        Assert.Equal(10.5, result.TotalUsers.TrendPercentage);
        Assert.Equal(50, result.ActiveQuizzes.Value);
        Assert.Equal(5.2, result.ActiveQuizzes.TrendPercentage);
        Assert.Equal(1234, result.Revenue.Value);
        Assert.Equal(12.3, result.Revenue.TrendPercentage);
        Assert.Equal(25, result.Reports.Value);
        Assert.Equal(-3.1, result.Reports.TrendPercentage);
    }

    [Fact]
    public async Task GetUserEngagementChartData_CallsCorrectFunction()
    {
        // Arrange
        List<ChartDataDTO> chartData = [
            new() { Label = "2025-07-25", Value = 10 }
        ];

        string expectedQuery = string.Format(SqlConstants.CHART_FUNCTION_CALL_TEMPLATE, SqlConstants.GET_USER_ENGAGEMENT_CHART_DATA);

        _sqlQueryRepoMock.Setup(r => r.SqlQueryListAsync<ChartDataDTO>(
            expectedQuery,
            It.IsAny<NpgsqlParameter>(),
            It.IsAny<NpgsqlParameter>()))
            .ReturnsAsync(chartData);

        AdminDashboardService service = CreateService();

        // Act
        List<ChartDataDTO> result = await service.GetUserEngagementChartData("2025-07-01", "2025-07-31");

        // Assert
        Assert.Single(result);
        Assert.Equal("2025-07-25", result[0].Label);
        Assert.Equal(10, result[0].Value);
    }

    [Fact]
    public async Task GetRevenueTrendChartData_CallsCorrectFunction()
    {
        // Arrange
        List<ChartDataDTO> chartData = [
            new() { Label = "2025-07-25", Value = 200 }
        ];

        string expectedQuery = string.Format(SqlConstants.CHART_FUNCTION_CALL_TEMPLATE, SqlConstants.GET_REVENUE_TREND_CHART_DATA);

       _sqlQueryRepoMock.Setup(r => r.SqlQueryListAsync<ChartDataDTO>(
            expectedQuery,
            It.IsAny<NpgsqlParameter>(),
            It.IsAny<NpgsqlParameter>()))
            .ReturnsAsync(chartData);

        AdminDashboardService service = CreateService();

        // Act
        List<ChartDataDTO> result = await service.GetRevenueTrendChartData("2025-07-01", "2025-07-31");

        // Assert
        Assert.Single(result);
        Assert.Equal("2025-07-25", result[0].Label);
        Assert.Equal(200, result[0].Value);
    }

    [Fact]
    public async Task GetPerformanceScoreChartData_CallsCorrectFunction()
    {
        // Arrange
        List<ChartDataDTO> chartData = [
            new() { Label = "2025-07-25", Value = 75 }
        ];

        string expectedQuery = string.Format(SqlConstants.CHART_FUNCTION_CALL_TEMPLATE, SqlConstants.GET_PERFORMANCE_SCORE_CHART_DATA);

        _sqlQueryRepoMock.Setup(r => r.SqlQueryListAsync<ChartDataDTO>(
            expectedQuery,
            It.IsAny<NpgsqlParameter>(),
            It.IsAny<NpgsqlParameter>()))
            .ReturnsAsync(chartData);

        AdminDashboardService service = CreateService();

        // Act
        List<ChartDataDTO> result = await service.GetPerformanceScoreChartData("2025-07-01", "2025-07-31");

        // Assert
        Assert.Single(result);
        Assert.Equal("2025-07-25", result[0].Label);
        Assert.Equal(75, result[0].Value);
    }
}
