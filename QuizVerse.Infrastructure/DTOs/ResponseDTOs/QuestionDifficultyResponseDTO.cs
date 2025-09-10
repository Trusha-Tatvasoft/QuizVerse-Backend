namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionDifficultyResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int XpGained { get; set; }
    public int TotalQuestions { get; set;}
}
