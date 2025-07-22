using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class Quiz
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int TotalTime { get; set; }

    public int TotalQuestion { get; set; }

    public int NoOfPersonAttempted { get; set; }

    public bool IsPaid { get; set; }

    public decimal Price { get; set; }

    public decimal Rating { get; set; }

    public int NoOfAttempts { get; set; }

    public int TotalXp { get; set; }

    public int Status { get; set; }

    public int DifficultyLevelId { get; set; }

    public bool IsFeatured { get; set; }

    public int CategoryId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<BattleList> BattleLists { get; set; } = new List<BattleList>();

    public virtual QuizCategory Category { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual QuizDifficulty DifficultyLevel { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<QuestionIssueReport> QuestionIssueReports { get; set; } = new List<QuestionIssueReport>();

    public virtual ICollection<QuizAttempted> QuizAttempteds { get; set; } = new List<QuizAttempted>();

    public virtual ICollection<QuizPurchased> QuizPurchaseds { get; set; } = new List<QuizPurchased>();

    public virtual ICollection<QuizRating> QuizRatings { get; set; } = new List<QuizRating>();

    public virtual ICollection<QuizTagMapping> QuizTagMappings { get; set; } = new List<QuizTagMapping>();

    public virtual ICollection<QuizToBaseQuestionMap> QuizToBaseQuestionMaps { get; set; } = new List<QuizToBaseQuestionMap>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizzes { get; set; } = new List<UserFavoriteQuiz>();
}
