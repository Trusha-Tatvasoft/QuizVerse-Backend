using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserPerformanceDetail
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int TotalXp { get; set; }

    public int OldGlobalRank { get; set; }

    public int NewGlobalRank { get; set; }

    public int CurrentLevel { get; set; }

    public int CurrentStreak { get; set; }

    public int HighestStreak { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual User User { get; set; } = null!;
}
