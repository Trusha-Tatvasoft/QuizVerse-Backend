using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuestionType
{
    public int id { get; set; }

    public string type_name { get; set; } = null!;

    public string? description { get; set; }

    public virtual ICollection<BaseQuestion> BaseQuestions { get; set; } = new List<BaseQuestion>();
}
