namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class AdminProfileResponseDto
{
    public string FullName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfilePic { get; set; }
}
