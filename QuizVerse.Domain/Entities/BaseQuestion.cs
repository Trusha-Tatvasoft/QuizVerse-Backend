using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BaseQuestion
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public int QueDifficultyId { get; set; }

    public string QueText { get; set; } = null!;

    public int QueTypeId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual QuizCategory Category { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual QuestionDifficulty QueDifficulty { get; set; } = null!;

    public virtual ICollection<QueOptionsAn> QueOptionsAns { get; set; } = new List<QueOptionsAn>();

    public virtual QuestionType QueType { get; set; } = null!;

    public virtual ICollection<QuestionIssueReport> QuestionIssueReports { get; set; } = new List<QuestionIssueReport>();

    public virtual ICollection<QuizToBaseQuestionMap> QuizToBaseQuestionMaps { get; set; } = new List<QuizToBaseQuestionMap>();
}
