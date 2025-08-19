using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuestionType
{
    public int Id { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<BaseQuestion> BaseQuestions { get; set; } = new List<BaseQuestion>();
}
