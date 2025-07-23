using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserBadgesEarned
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BadgeId { get; set; }

    public DateTime DateEarned { get; set; }

    public virtual Badge Badge { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
