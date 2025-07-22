using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizDifficulty
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string description { get; set; } = null!;

    public bool is_deleted { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
