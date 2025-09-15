using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizToBaseQuestionMap
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int QueId { get; set; }

    public bool IsDeleted { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<AttemptedQuizQuestionsAnswer> AttemptedQuizQuestionsAnswers { get; set; } = new List<AttemptedQuizQuestionsAnswer>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual BaseQuestion Que { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;
}
