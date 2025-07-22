using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizTag
{
    public int Id { get; set; }

    public string TagName { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<QuizTagMapping> QuizTagMappings { get; set; } = new List<QuizTagMapping>();
}
