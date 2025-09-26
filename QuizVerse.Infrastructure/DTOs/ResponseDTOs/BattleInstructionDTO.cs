namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class BattleInstructionDTO
{
    public int BattleAttemptId { get; set; }
    public string BattleName { get; set; } = null!;
    public string BattleDescription { get; set; } = null!;
    public string BattleCategory { get; set; } = null!;
    public decimal TimeInSeconds { get; set; }
}