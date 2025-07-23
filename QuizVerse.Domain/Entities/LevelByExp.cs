using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class LevelByExp
{
    public int Id { get; set; }

    public int LevelOrder { get; set; }

    public int MinimumExp { get; set; }

    public int MaximumExp { get; set; }
}
