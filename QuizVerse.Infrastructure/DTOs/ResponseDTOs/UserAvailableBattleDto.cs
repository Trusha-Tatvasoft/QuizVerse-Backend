namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserAvailableBattleDto
{
    public int BattleId { get; set; }
    public string BattleName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int MaxXP { get; set; }
    public int TotalQuestions { get; set; }
    public TimeSpan Duration { get; set; }
    public int Participants { get; set; }
    public bool IsBattleRunning { get; set; }
}
