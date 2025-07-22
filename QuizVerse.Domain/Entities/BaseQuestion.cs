using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BaseQuestion
{
    public int id { get; set; }

    public int category_id { get; set; }

    public int que_difficulty_id { get; set; }

    public string que_text { get; set; } = null!;

    public int que_type_id { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<QueOptionsAn> QueOptionsAns { get; set; } = new List<QueOptionsAn>();

    public virtual ICollection<QuestionIssueReport> QuestionIssueReports { get; set; } = new List<QuestionIssueReport>();

    public virtual ICollection<QuizToBaseQuestionMap> QuizToBaseQuestionMaps { get; set; } = new List<QuizToBaseQuestionMap>();

    public virtual QuizCategory category { get; set; } = null!;

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }

    public virtual QuestionDifficulty que_difficulty { get; set; } = null!;

    public virtual QuestionType que_type { get; set; } = null!;
}
