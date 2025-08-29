namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionDifficultyXPData
{
    public int QuestionDifficultyId { get; set; }
    public string QuestionDifficultyName { get; set; } = null!;
    public int XpGained { get; set; }
}