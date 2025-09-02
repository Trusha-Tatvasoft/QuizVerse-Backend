namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserBasicProfileDto
{
    public int UserId { get; set; }
    public string ProfilePic { get; set; } = string.Empty;
    public string Name { get; set; } = null!;
    public string Rank { get; set; } = null!;
    public string NextRank { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public decimal Progress { get; set; }
    public int TotalXp { get; set; }
    public int QuizCompleted { get; set; }
    public decimal WinRate { get; set; }
    public int Achievements { get; set; }
}
