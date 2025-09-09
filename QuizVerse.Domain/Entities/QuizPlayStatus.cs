using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizPlayStatus
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int QuizId { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<AttemptedQuizQuestionsAnswer> AttemptedQuizQuestionsAnswers { get; set; } = new List<AttemptedQuizQuestionsAnswer>();

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
