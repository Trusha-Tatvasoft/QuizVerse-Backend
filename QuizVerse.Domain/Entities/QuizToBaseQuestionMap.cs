using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizToBaseQuestionMap
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int QueId { get; set; }

    public virtual BaseQuestion Que { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;
}
