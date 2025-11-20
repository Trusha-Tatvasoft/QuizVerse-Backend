namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizReportIssueResponseDTO
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = null!;
    public string Creator { get; set; } = null!;
    public string Reporter { get; set; } = null!;
    public int ReviewedBy { get; set; }
    public string Reason { get; set; } = null!;
    public int Severity { get; set; }
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }
}
