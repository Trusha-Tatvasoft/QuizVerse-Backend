using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizToQuestionDifficultyMap
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int QuestionTypeId { get; set; }

    public int NoOfQuestions { get; set; }
    public bool IsDeleted { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual QuestionType QuestionType { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;
}
