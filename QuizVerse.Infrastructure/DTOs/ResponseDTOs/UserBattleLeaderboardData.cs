namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserBattleLeaderboardData
{
    public string UserName { get; set; } = null!;
    public int TotalWins { get; set; }
    public decimal WinPercentage { get; set; }
    public long TotalXp { get; set; }
    public int Rank { get; set; }
    public bool IsLoggedInUser { get; set; } = false;
}