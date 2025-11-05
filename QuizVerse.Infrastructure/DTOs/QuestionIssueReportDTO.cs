namespace QuizVerse.Infrastructure.DTOs;

public class QuestionIssueReportDTO
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Question { get; set; } = null!;
    public string Creator { get; set; } = null!;
    public string Reporter { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public int Severity { get; set; }
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }
}
