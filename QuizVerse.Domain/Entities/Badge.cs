using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class Badge
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryType { get; set; }

    public string Description { get; set; } = null!;

    public int Xp { get; set; }

    public int BadgeType { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual ICollection<BadgeConditionsMapping> BadgeConditionsMappings { get; set; } = new List<BadgeConditionsMapping>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<UserBadgesEarned> UserBadgesEarneds { get; set; } = new List<UserBadgesEarned>();
}
