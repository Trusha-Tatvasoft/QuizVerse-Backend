using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuestionIssueReport
{
    public int id { get; set; }

    public int user_id { get; set; }

    public string description { get; set; } = null!;

    public int quiz_id { get; set; }

    public int question_id { get; set; }

    public bool is_resolved { get; set; }

    public virtual BaseQuestion question { get; set; } = null!;

    public virtual Quiz quiz { get; set; } = null!;

    public virtual User user { get; set; } = null!;
}
