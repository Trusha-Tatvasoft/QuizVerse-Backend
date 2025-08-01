namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserExportDto
{
    public int No { get; set; } 
    public string? FullName { get; set; } 
    public string? Email { get; set; } 
    public string? UserName { get; set; } 
    public int TotalQuizAttemptedCount { get; set; }
    public string? RoleName { get; set; } 
    public string? StatusName { get; set; } 
    public string JoinDate { get; set; } = "-"; 
    public string LastActive { get; set; } = "-";
}
