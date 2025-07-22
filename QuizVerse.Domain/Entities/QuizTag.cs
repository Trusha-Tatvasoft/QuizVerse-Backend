using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizTag
{
    public int id { get; set; }

    public string tag_name { get; set; } = null!;

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<QuizTagMapping> QuizTagMappings { get; set; } = new List<QuizTagMapping>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
