using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizDifficulty
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
