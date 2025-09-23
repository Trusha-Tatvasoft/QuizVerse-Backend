namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserBattleHistoryDto
{
    public string BattleName { get; set; } = null!;
    public string Opponent { get; set; } = null!;
    public string OpponentFullName { get; set; } = null!;
    public string? ProfilePic { get; set; } = string.Empty;
    public string Category { get; set; } = null!;
    public string Result { get; set; } = null!;
    public int YourScore { get; set; }
    public int OpponentScore { get; set; }
    public int XpGained { get; set; }
    public DateTime BattleDate { get; set; }
}

public class UserBattleHistoryResponseDto
{
    public List<UserBattleHistoryDto> Battles { get; set; } = new();
    public bool HasMore { get; set; }
}