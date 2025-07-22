using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class GradeForQuizResult
{
    public int id { get; set; }

    public decimal min_percentage { get; set; }

    public decimal max_percentage { get; set; }

    public string grade { get; set; } = null!;

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<QuizAttempted> QuizAttempteds { get; set; } = new List<QuizAttempted>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
