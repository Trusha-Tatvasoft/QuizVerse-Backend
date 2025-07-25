using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs;

public class UpdateUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
}
