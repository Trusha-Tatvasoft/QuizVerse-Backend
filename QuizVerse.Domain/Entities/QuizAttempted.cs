using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizAttempted
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int UserId { get; set; }

    public int TotalQue { get; set; }

    public int CorrectedQue { get; set; }

    public TimeSpan TimeSpent { get; set; }

    public int XpEarned { get; set; }

    public int Grade { get; set; }

    public int AttemptLeft { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual GradeForQuizResult GradeNavigation { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
