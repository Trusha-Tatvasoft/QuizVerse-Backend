namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class PlayerProfileDTO
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public double WinRate { get; set; }
    public string ProfilePic { get; set; } = string.Empty;
}
