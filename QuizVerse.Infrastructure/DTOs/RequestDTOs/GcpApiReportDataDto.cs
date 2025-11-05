using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class GcpApiReportDataDto
{
    public int ReportId { get; set; }
    public ReportType ReportType { get; set; }
    public string ReportComment { get; set; } = null!;
    public string? CacheKey { get; set; }
}
public class GcpApiReportAllDataDto
{
    public int ReportId { get; set; }
    public int ReportType { get; set; }
    public string ReportComment { get; set; } = null!;
}
