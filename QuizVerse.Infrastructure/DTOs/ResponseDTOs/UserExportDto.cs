namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserExportDto
{
    public string? FullName { get; set; } 
    public string? Email { get; set; } 
    public string? UserName { get; set; } 
    public int TotalQuizAttemptedCount { get; set; }
    public string? RoleName { get; set; } 
    public string? StatusName { get; set; } 
    public DateTime JoinDate { get; set; }
    public DateTime? LastActive { get; set; }
}
