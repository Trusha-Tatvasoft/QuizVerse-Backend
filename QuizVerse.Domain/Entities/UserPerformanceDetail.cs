using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserPerformanceDetail
{
    public int id { get; set; }

    public int user_id { get; set; }

    public int total_xp { get; set; }

    public int old_global_rank { get; set; }

    public int new_global_rank { get; set; }

    public int current_level { get; set; }

    public int current_streak { get; set; }

    public int highest_streak { get; set; }

    public DateTime created_date { get; set; }

    public DateTime? modified_date { get; set; }

    public virtual User user { get; set; } = null!;
}
