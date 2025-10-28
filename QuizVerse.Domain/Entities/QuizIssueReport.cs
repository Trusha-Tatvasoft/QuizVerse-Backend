using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizIssueReport
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int QuizId { get; set; }

    public string Reason { get; set; } = null!;

    public int Severity { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
