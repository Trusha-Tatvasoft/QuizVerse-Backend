using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class GradeForQuizResult
{
    public int Id { get; set; }

    public decimal MinPercentage { get; set; }

    public decimal MaxPercentage { get; set; }

    public string Grade { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<QuizAttempted> QuizAttempteds { get; set; } = new List<QuizAttempted>();
}
