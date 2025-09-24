namespace QuizVerse.Infrastructure.DTOs;

public class MatchmakingPlayerDTO
{
    public string ConnectionId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int BattleId { get; set; }
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
}
