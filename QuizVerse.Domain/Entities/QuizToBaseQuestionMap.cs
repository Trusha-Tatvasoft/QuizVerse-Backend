using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizToBaseQuestionMap
{
    public int id { get; set; }

    public int quiz_id { get; set; }

    public int que_id { get; set; }

    public virtual BaseQuestion que { get; set; } = null!;

    public virtual Quiz quiz { get; set; } = null!;
}
