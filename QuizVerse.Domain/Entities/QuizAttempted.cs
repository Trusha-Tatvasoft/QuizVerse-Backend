using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizAttempted
{
    public int id { get; set; }

    public int quiz_id { get; set; }

    public int user_id { get; set; }

    public int total_que { get; set; }

    public int corrected_que { get; set; }

    public TimeSpan time_spent { get; set; }

    public int xp_earned { get; set; }

    public int grade { get; set; }

    public int attempt_left { get; set; }

    public DateTime created_date { get; set; }

    public virtual GradeForQuizResult gradeNavigation { get; set; } = null!;

    public virtual Quiz quiz { get; set; } = null!;

    public virtual User user { get; set; } = null!;
}
