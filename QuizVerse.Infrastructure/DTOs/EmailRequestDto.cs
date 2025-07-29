namespace QuizVerse.Infrastructure.DTOs;

public class EmailRequestDto
{
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public List<string>? Cc { get; set; }
    public List<string>? Bcc { get; set; }
}