using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class PlatformConfiguration
{
    public int id { get; set; }

    public string configuration_name { get; set; } = null!;

    public string description { get; set; } = null!;

    public string values { get; set; } = null!;

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual User? modified_byNavigation { get; set; }
}
