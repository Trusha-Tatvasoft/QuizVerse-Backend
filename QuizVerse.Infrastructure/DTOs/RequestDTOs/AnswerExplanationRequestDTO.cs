namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class AnswerExplanationRequestDTO
{
    public string QuestionText { get; set; } = null!;
    public string? UserAnswer { get; set; }
    public string CorrectAnswer { get; set; } = null!;
}
