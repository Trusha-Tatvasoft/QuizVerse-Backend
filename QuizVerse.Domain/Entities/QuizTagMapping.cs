using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizTagMapping
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int TagId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual QuizTag Tag { get; set; } = null!;
}
