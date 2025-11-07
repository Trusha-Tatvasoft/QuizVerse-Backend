using QuizVerse.Domain.Entities;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizCommentsResponseDto
{
    public List<QuizCommentsDto> Comments { get; set; } = [];
    public bool hasMoreComments { get; set; }
}

public class QuizCommentsDto
{
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? ProfilePic { get; set; }
    public DateTime CommentDate { get; set; }
    public string? CommentText { get; set; } = null!;
    public decimal Rating { get; set; }
    public bool isUser { get; set; }
}