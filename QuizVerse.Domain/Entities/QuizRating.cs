using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizRating
{
    public int id { get; set; }

    public int quiz_id { get; set; }

    public int user_id { get; set; }

    public int quiz_rating { get; set; }

    public string? feedback { get; set; }

    public DateTime created_date { get; set; }

    public virtual Quiz quiz { get; set; } = null!;

    public virtual User user { get; set; } = null!;
}
