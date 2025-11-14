namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionIssueReportResponseDTO
{
    public string Description { get; set; } = string.Empty;
    public int QuizId { get; set; }
    public int QuestionId { get; set; }
    public int ReportId { get; set; }
}
