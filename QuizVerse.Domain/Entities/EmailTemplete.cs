using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class EmailTemplete
{
    public int id { get; set; }

    public int template_type { get; set; }

    public string title { get; set; } = null!;

    public string subject { get; set; } = null!;

    public string body { get; set; } = null!;

    public bool status { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
