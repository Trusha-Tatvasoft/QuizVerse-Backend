using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BadgeConditionsMapping
{
    public int Id { get; set; }

    public int BadgeId { get; set; }

    public int ConditionType { get; set; }

    public string ConditionValue { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Badge Badge { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }
}
