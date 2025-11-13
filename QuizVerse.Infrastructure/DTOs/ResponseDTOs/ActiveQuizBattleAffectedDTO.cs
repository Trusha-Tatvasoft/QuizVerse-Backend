
namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class ActiveQuizBattleAffectedDTO
{
    public int Id { get; set; }
    public string QuizTitle { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string QuizDifficultyLevel { get; set; } = null!;
    public int TotalQuestion { get; set; }
    public int Type { get; set; }
}