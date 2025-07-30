using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class AdminDashboardResponse
{
    public MetricSummaryDTO TotalUsers { get; set; } = new();
    public MetricSummaryDTO ActiveQuizzes { get; set; } = new();
    public MetricSummaryDTO Revenue { get; set; } = new();
    public MetricSummaryDTO Reports { get; set; } = new();
}

public class MetricSummaryDTO
{
    public int Value { get; set; }
    public double TrendPercentage { get; set; }
}

[Keyless]
public class RawAdminDashboardMetricsDTO
{
    [Column("total_users")]
    public int TotalUsers { get; set; }

    [Column("user_growth_percent")]
    public decimal UserGrowthPercent { get; set; }

    [Column("active_quizzes")]
    public int ActiveQuizzes { get; set; }

    [Column("quiz_growth_percent")]
    public decimal QuizGrowthPercent { get; set; }

    [Column("total_revenue")]
    public int TotalRevenue { get; set; }

    [Column("revenue_growth_percent")]
    public decimal RevenueGrowthPercent { get; set; }

    [Column("total_reports")]
    public int TotalReports { get; set; }

    [Column("report_growth_percent")]
    public decimal ReportGrowthPercent { get; set; }
}

[Keyless]
public class ChartDataDTO
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}
