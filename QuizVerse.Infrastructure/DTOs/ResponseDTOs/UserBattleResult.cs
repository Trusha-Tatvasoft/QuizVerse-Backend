namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserBattleResult
{
    public string BattleName { get; set; } = null!;
    public string OpponentUserName { get; set; } = null!;
    public string? PlayerProfile { get; set; }
    public string? OpponentProfile { get; set; }
    public int BattleStatus { get; set; }
    public bool IsWin { get; set; }
    public int PlayerAttemptedQuestions { get; set; }
    public int OpponentAttemptedQuestions { get; set; }
    public int PlayerEarnedXP { get; set; }
}
