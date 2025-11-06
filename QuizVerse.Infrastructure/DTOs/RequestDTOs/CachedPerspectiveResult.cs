using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class CachedPerspectiveResult
{
    public QuestionOrQuizIssueReportSeverity Severity { get; set; }
    public bool IsFlagged { get; set; }
    public string? FlagReason { get; set; }
    public double AttributeScore { get; set; }
    public ReportType ReportType { get; set; }
    public DateTime CachedAt { get; set; }
}
