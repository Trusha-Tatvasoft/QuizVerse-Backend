using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserRankByLevel
{
    public int Id { get; set; }

    public string RankName { get; set; } = null!;

    public int MinimumLevel { get; set; }

    public int MaximumLevel { get; set; }
}
