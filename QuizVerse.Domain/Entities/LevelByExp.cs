using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class LevelByExp
{
    public int id { get; set; }

    public int level_order { get; set; }

    public int minimum_exp { get; set; }

    public int maximum_exp { get; set; }
}
