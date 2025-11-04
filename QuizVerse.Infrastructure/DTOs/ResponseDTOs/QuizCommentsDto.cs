namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizCommentsDto
{
    public string UserName { get; set; } = null!;
    public string? ProfilePic { get; set; }
    public DateTime CommentDate { get; set; }
    public string CommentText { get; set; } = null!;
    public decimal Rating { get; set; }
}