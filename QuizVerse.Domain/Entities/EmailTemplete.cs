using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class EmailTemplete
{
    public int Id { get; set; }

    public int TemplateType { get; set; }

    public string Title { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public bool Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }
}
