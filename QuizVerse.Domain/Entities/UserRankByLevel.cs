using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserRankByLevel
{
    public int id { get; set; }

    public string rank_name { get; set; } = null!;

    public int minimum_level { get; set; }

    public int maximum_level { get; set; }
}
