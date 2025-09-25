namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class SearchUserResponseDto
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePic { get; set; }
    public int TotalXp { get; set; }
}
