using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

[Keyless]
public class RawLeaderboardGlobalRankingDto
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

    [Column("current_level")]
    public int CurrentLevel { get; set; }

    [Column("current_streak")]
    public int CurrentStreak { get; set; }

    [Column("trend")]
    public int Trend { get; set; }   // 1 = same, 2 = up, 3 = down

    [Column("is_loggedin_user")]
    public bool IsLoggedInUser { get; set; }  // true if this row is the requested user
}

public class LeaderboardGlobalRankingResponseDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePic { get; set; }
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; }
    public int CurrentStreak { get; set; }
    public int Trend { get; set; }   // 1 = same, 2 = up, 3 = down
    public bool IsLoggedInUser { get; set; }
}