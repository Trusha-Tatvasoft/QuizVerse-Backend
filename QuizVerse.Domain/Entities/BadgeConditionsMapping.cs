using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BadgeConditionsMapping
{
    public int Id { get; set; }

    public int BadgeId { get; set; }

    public int ConditionType { get; set; }

    public string ConditionValue { get; set; } = null!;

    public virtual Badge Badge { get; set; } = null!;
}
