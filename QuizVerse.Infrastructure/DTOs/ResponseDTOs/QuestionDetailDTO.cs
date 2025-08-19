namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionDetailDTO
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<QuestionOptionDTO>? Options { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
}

public class QuestionOptionDTO
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
