namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class ExportQuizQuestionsRequestDto
{
    public string QuizName { get; set; } = null!;
    public List<QuestionsListRequestDto> Questions { get; set; } = new();
}