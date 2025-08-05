namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int RoleId { get; set; }
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastLogin { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePic { get; set; }
    public int AttemptedQuizzes { get; set; }
}