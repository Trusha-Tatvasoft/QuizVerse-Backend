using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuestionIssueReport
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Description { get; set; } = null!;

    public int QuizId { get; set; }

    public int QuestionId { get; set; }

    public bool IsResolved { get; set; }

    public virtual BaseQuestion Question { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
