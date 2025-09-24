namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class MatchmakingResultDTO
{
    public bool IsMatched { get; set; }
    public MatchmakingPlayerDTO? Player { get; set; }
    public MatchmakingPlayerDTO? Opponent { get; set; }
    public PlayerProfileDTO? PlayerProfile { get; set; }
    public PlayerProfileDTO? OpponentProfile { get; set; }
}
