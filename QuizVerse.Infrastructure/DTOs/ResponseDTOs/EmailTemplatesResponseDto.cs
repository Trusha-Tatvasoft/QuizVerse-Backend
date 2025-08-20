namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class EmailTemplatesResponseDto
{
    public int Id { get; set; }
    public int TemplateType { get; set; }
    public string Title { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool Status { get; set; }
}