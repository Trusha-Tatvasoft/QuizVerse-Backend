using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class PerspectiveAnalysisResult
{
    public int ReportId { get; set; }
    public ReportType ReportType { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public QuestionOrQuizIssueReportSeverity Severity { get; set; }
    public bool IsFlagged { get; set; } 
    public string? FlagReason { get; set; }
    public string? AttributeName { get; set; } 
    public double AttributeScore { get; set; }
}

public class PerspectiveApiResponse
{
    public Dictionary<string, AttributeScore>? AttributeScores { get; set; }
}

public class AttributeScore
{
    public SummaryScore? SummaryScore { get; set; }
}

public class SummaryScore
{
    public double Value { get; set; }
}