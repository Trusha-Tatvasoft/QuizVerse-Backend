using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizCategory
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Icon { get; set; }

    public bool Status { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual ICollection<BaseQuestion> BaseQuestions { get; set; } = new List<BaseQuestion>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
