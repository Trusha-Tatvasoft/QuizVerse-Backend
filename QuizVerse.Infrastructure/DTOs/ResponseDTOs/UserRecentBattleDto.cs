namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserRecentBattleDto
{
    public string Opponent { get; set; } = null!;
    public string? ProfilePic { get; set; } = string.Empty;
    public string Category { get; set; } = null!;
    public string Result { get; set; } = null!;
    public int YourScore { get; set; }
    public int OpponentScore { get; set; }
    public int XpGained { get; set; }
}
