using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class PlatformConfiguration
{
    public int Id { get; set; }

    public string ConfigurationName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Values { get; set; } = null!;

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User? ModifiedByNavigation { get; set; }
}
