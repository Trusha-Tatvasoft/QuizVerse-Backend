using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserBadgesEarned
{
    public int id { get; set; }

    public int user_id { get; set; }

    public int badge_id { get; set; }

    public DateTime date_earned { get; set; }

    public virtual Badge badge { get; set; } = null!;

    public virtual User user { get; set; } = null!;
}
