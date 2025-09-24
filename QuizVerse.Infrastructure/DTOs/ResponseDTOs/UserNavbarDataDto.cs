namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserNavbarDataDto
{
    public int? CurrentUserXp { get; set; }
    public decimal? ProgressPercentage { get; set; }
    public string? ProfilePic { get; set; }
    public int NotificationCount { get; set; } = 0;
}