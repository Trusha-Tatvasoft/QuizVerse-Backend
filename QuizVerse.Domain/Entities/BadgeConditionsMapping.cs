using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BadgeConditionsMapping
{
    public int id { get; set; }

    public int badge_id { get; set; }

    public int condition_type { get; set; }

    public string condition_value { get; set; } = null!;

    public virtual Badge badge { get; set; } = null!;
}
