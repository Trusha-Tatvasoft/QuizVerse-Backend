using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class AttemptedQuizQuestionsAnswer
{
    public long Id { get; set; }

    public int QuizPlayStatusId { get; set; }

    public int QuizQueId { get; set; }

    public int QueTypeId { get; set; }

    public string? GivenAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual QuestionType QueType { get; set; } = null!;

    public virtual QuizPlayStatus QuizPlayStatus { get; set; } = null!;

    public virtual QuizToBaseQuestionMap QuizQue { get; set; } = null!;
}
