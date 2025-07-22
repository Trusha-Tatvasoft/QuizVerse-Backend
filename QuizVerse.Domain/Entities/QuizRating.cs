using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizRating
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int UserId { get; set; }

    public int QuizRating1 { get; set; }

    public string? Feedback { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
