using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int RoleId { get; set; }

    public string? Bio { get; set; }

    public string? ProfilePic { get; set; }

    public DateTime LastLogin { get; set; }

    public bool FirstTimeLogin { get; set; }

    public bool IsDeleted { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual ICollection<Badge> BadgeCreatedByNavigations { get; set; } = new List<Badge>();

    public virtual ICollection<Badge> BadgeModifiedByNavigations { get; set; } = new List<Badge>();

    public virtual ICollection<BaseQuestion> BaseQuestionCreatedByNavigations { get; set; } = new List<BaseQuestion>();

    public virtual ICollection<BaseQuestion> BaseQuestionModifiedByNavigations { get; set; } = new List<BaseQuestion>();

    public virtual ICollection<BattleList> BattleListCreatedByNavigations { get; set; } = new List<BattleList>();

    public virtual ICollection<BattleList> BattleListModifiedByNavigations { get; set; } = new List<BattleList>();

    public virtual ICollection<BattleRequest> BattleRequestReceivers { get; set; } = new List<BattleRequest>();

    public virtual ICollection<BattleRequest> BattleRequestSenders { get; set; } = new List<BattleRequest>();

    public virtual ICollection<BattleResult> BattleResults { get; set; } = new List<BattleResult>();

    public virtual ICollection<BattleStatus> BattleStatusUser1s { get; set; } = new List<BattleStatus>();

    public virtual ICollection<BattleStatus> BattleStatusUser2s { get; set; } = new List<BattleStatus>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<EmailTemplete> EmailTempleteCreatedByNavigations { get; set; } = new List<EmailTemplete>();

    public virtual ICollection<EmailTemplete> EmailTempleteModifiedByNavigations { get; set; } = new List<EmailTemplete>();

    public virtual ICollection<GlobalPayment> GlobalPaymentCreatedByNavigations { get; set; } = new List<GlobalPayment>();

    public virtual ICollection<GlobalPayment> GlobalPaymentModifiedByNavigations { get; set; } = new List<GlobalPayment>();

    public virtual ICollection<GlobalPayment> GlobalPaymentPaidByUsers { get; set; } = new List<GlobalPayment>();

    public virtual ICollection<GradeForQuizResult> GradeForQuizResultCreatedByNavigations { get; set; } = new List<GradeForQuizResult>();

    public virtual ICollection<GradeForQuizResult> GradeForQuizResultModifiedByNavigations { get; set; } = new List<GradeForQuizResult>();

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<User> InverseModifiedByNavigation { get; set; } = new List<User>();

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationModifiedByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentPlatform> PaymentPlatformCreatedByNavigations { get; set; } = new List<PaymentPlatform>();

    public virtual ICollection<PaymentPlatform> PaymentPlatformModifiedByNavigations { get; set; } = new List<PaymentPlatform>();

    public virtual ICollection<PaymentType> PaymentTypeCreatedByNavigations { get; set; } = new List<PaymentType>();

    public virtual ICollection<PaymentType> PaymentTypeModifiedByNavigations { get; set; } = new List<PaymentType>();

    public virtual ICollection<PlatformConfiguration> PlatformConfigurations { get; set; } = new List<PlatformConfiguration>();

    public virtual ICollection<QueOptionsAn> QueOptionsAnCreatedByNavigations { get; set; } = new List<QueOptionsAn>();

    public virtual ICollection<QueOptionsAn> QueOptionsAnModifiedByNavigations { get; set; } = new List<QueOptionsAn>();

    public virtual ICollection<QuestionDifficulty> QuestionDifficultyCreatedByNavigations { get; set; } = new List<QuestionDifficulty>();

    public virtual ICollection<QuestionDifficulty> QuestionDifficultyModifiedByNavigations { get; set; } = new List<QuestionDifficulty>();

    public virtual ICollection<QuestionIssueReport> QuestionIssueReports { get; set; } = new List<QuestionIssueReport>();

    public virtual ICollection<QuizAttempted> QuizAttempteds { get; set; } = new List<QuizAttempted>();

    public virtual ICollection<QuizCategory> QuizCategoryCreatedByNavigations { get; set; } = new List<QuizCategory>();

    public virtual ICollection<QuizCategory> QuizCategoryModifiedByNavigations { get; set; } = new List<QuizCategory>();

    public virtual ICollection<Quiz> QuizCreatedByNavigations { get; set; } = new List<Quiz>();

    public virtual ICollection<QuizDifficulty> QuizDifficultyCreatedByNavigations { get; set; } = new List<QuizDifficulty>();

    public virtual ICollection<QuizDifficulty> QuizDifficultyModifiedByNavigations { get; set; } = new List<QuizDifficulty>();

    public virtual ICollection<Quiz> QuizModifiedByNavigations { get; set; } = new List<Quiz>();

    public virtual ICollection<QuizPurchased> QuizPurchaseds { get; set; } = new List<QuizPurchased>();

    public virtual ICollection<QuizRating> QuizRatings { get; set; } = new List<QuizRating>();

    public virtual ICollection<QuizTag> QuizTagCreatedByNavigations { get; set; } = new List<QuizTag>();

    public virtual ICollection<QuizTagMapping> QuizTagMappingCreatedByNavigations { get; set; } = new List<QuizTagMapping>();

    public virtual ICollection<QuizTagMapping> QuizTagMappingModifiedByNavigations { get; set; } = new List<QuizTagMapping>();

    public virtual ICollection<QuizTag> QuizTagModifiedByNavigations { get; set; } = new List<QuizTag>();

    public virtual UserRole Role { get; set; } = null!;

    public virtual ICollection<UserBadgesEarned> UserBadgesEarneds { get; set; } = new List<UserBadgesEarned>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizCreatedByNavigations { get; set; } = new List<UserFavoriteQuiz>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizModifiedByNavigations { get; set; } = new List<UserFavoriteQuiz>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizUsers { get; set; } = new List<UserFavoriteQuiz>();

    public virtual ICollection<UserNotification> UserNotificationCreatedByNavigations { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserNotification> UserNotificationModifiedByNavigations { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserNotification> UserNotificationUsers { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserPerformanceDetail> UserPerformanceDetails { get; set; } = new List<UserPerformanceDetail>();
}
