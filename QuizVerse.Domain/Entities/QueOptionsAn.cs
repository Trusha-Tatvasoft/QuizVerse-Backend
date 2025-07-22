using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QueOptionsAn
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual BaseQuestion Question { get; set; } = null!;
}
