using System.ComponentModel.DataAnnotations.Schema;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class MonthlyChampionsResponseDto
{
    [Column("rank")]
    public int Rank { get; set; }
    [Column("user_id")]
    public int UserId { get; set; }
    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;
    [Column("profile_pic")]
    public string? ProfilePic { get; set; }
    [Column("total_xp")]
    public int TotalXp { get; set; }
    [Column("average_score")]
    public decimal AverageScore { get; set; }
    [Column("total_quizzes_played")]
    public int TotalQuizzesPlayed { get; set; }
    [Column("total_battles_played")]
    public int TotalBattlesPlayed { get; set; }
    [Column("is_logged_in_user")]
    public bool IsLoggedInUser { get; set; }
}