using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class Badge
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public int category_type { get; set; }

    public string description { get; set; } = null!;

    public int xp { get; set; }

    public int badge_type { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<BadgeConditionsMapping> BadgeConditionsMappings { get; set; } = new List<BadgeConditionsMapping>();

    public virtual ICollection<UserBadgesEarned> UserBadgesEarneds { get; set; } = new List<UserBadgesEarned>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
