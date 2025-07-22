using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class Quiz
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string description { get; set; } = null!;

    public int total_time { get; set; }

    public int total_question { get; set; }

    public int no_of_person_attempted { get; set; }

    public bool is_paid { get; set; }

    public decimal price { get; set; }

    public decimal rating { get; set; }

    public int no_of_attempts { get; set; }

    public int total_xp { get; set; }

    public int status { get; set; }

    public int difficulty_level_id { get; set; }

    public bool is_featured { get; set; }

    public int category_id { get; set; }

    public int created_by { get; set; }

    public DateTime created_date { get; set; }

    public int? modified_by { get; set; }

    public DateTime? modified_date { get; set; }

    public bool is_deleted { get; set; }

    public virtual ICollection<BattleList> BattleLists { get; set; } = new List<BattleList>();

    public virtual ICollection<QuestionIssueReport> QuestionIssueReports { get; set; } = new List<QuestionIssueReport>();

    public virtual ICollection<QuizAttempted> QuizAttempteds { get; set; } = new List<QuizAttempted>();

    public virtual ICollection<QuizPurchased> QuizPurchaseds { get; set; } = new List<QuizPurchased>();

    public virtual ICollection<QuizRating> QuizRatings { get; set; } = new List<QuizRating>();

    public virtual ICollection<QuizTagMapping> QuizTagMappings { get; set; } = new List<QuizTagMapping>();

    public virtual ICollection<QuizToBaseQuestionMap> QuizToBaseQuestionMaps { get; set; } = new List<QuizToBaseQuestionMap>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizzes { get; set; } = new List<UserFavoriteQuiz>();

    public virtual QuizCategory category { get; set; } = null!;

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual QuizDifficulty difficulty_level { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
