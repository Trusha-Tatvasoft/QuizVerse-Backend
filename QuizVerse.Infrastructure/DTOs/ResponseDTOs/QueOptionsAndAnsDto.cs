namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QueOptionsAndAnsDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}