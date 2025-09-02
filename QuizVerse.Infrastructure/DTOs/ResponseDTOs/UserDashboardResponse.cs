using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserDashboardResponse
{
    public string UserName { get; set; } = string.Empty;
    public int QuizzesCompleted { get; set; }
    public int TotalXp { get; set; }
    public double WinRate { get; set; }
    public int CurrentRank { get; set; }
}

[Keyless]
public class RawUserDashboardMetricsDTO
{
    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Column("quizzes_completed")]
    public int QuizzesCompleted { get; set; }

    [Column("total_xp")]
    public int TotalXp { get; set; }

    [Column("win_rate")]
    public double WinRate { get; set; }

    [Column("current_rank")]
    public int CurrentRank { get; set; }
}

public class RecentQuizResponse
{
    public string QuizName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string DifficultyLevel { get; set; } = null!;
    public double Score { get; set; }
    public DateTime AttemptedOn { get; set; }
}

public class FeaturedQuizDTO
{
    public int QuizId { get; set; }
    public string QuizName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string DifficultyLevel { get; set; } = null!;
    public int TotalAttempts { get; set; }
    public decimal Rating { get; set; }
}

public class FeaturedQuizListDTO
{
    public List<FeaturedQuizDTO> Quizzes { get; set; } = [];
    public bool HasMore { get; set; }
}

public class BattleRequestDTO
{
    public int RequestId { get; set; }
    public string SenderUserName { get; set; } = null!;
    public string? SenderProfilePic { get; set; }
    public string? BattleName { get; set; } = null!;
    public string BattleCategory { get; set; } = null!;
    public string BattleDifficulty { get; set; } = null!;
    public DateTime SendingDate { get; set; }
    public string TimeAgo { get; set; } = null!;
}

public class RankProgressDTO
{
    public string CurrentRank { get; set; } = null!;
    public string? NextRank { get; set; }
    public int XpNeeded { get; set; }
    public decimal ProgressPercent { get; set; }
}

[Keyless]
public class RawRankProgressDTO
{
    [Column("current_rank")]
    public string CurrentRank { get; set; } = null!;

    [Column("next_rank")]
    public string? NextRank { get; set; }

    [Column("xp_needed")]
    public int XpNeeded { get; set; }

    [Column("progress_percent")]
    public decimal ProgressPercent { get; set; }
}
