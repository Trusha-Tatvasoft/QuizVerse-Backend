using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class User
{
    public int id { get; set; }

    public string full_name { get; set; } = null!;

    public string user_name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string password { get; set; } = null!;

    public int role_id { get; set; }

    public string? bio { get; set; }

    public string? profile_pic { get; set; }

    public DateTime last_login { get; set; }

    public bool first_time_login { get; set; }

    public bool is_deleted { get; set; }

    public int status { get; set; }

    public DateTime created_date { get; set; }

    public int? created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<Badge> Badgecreated_byNavigations { get; set; } = new List<Badge>();

    public virtual ICollection<Badge> Badgemodified_byNavigations { get; set; } = new List<Badge>();

    public virtual ICollection<BaseQuestion> BaseQuestioncreated_byNavigations { get; set; } = new List<BaseQuestion>();

    public virtual ICollection<BaseQuestion> BaseQuestionmodified_byNavigations { get; set; } = new List<BaseQuestion>();

    public virtual ICollection<BattleList> BattleListcreated_byNavigations { get; set; } = new List<BattleList>();

    public virtual ICollection<BattleList> BattleListmodified_byNavigations { get; set; } = new List<BattleList>();

    public virtual ICollection<BattleRequest> BattleRequestreceivers { get; set; } = new List<BattleRequest>();

    public virtual ICollection<BattleRequest> BattleRequestsenders { get; set; } = new List<BattleRequest>();

    public virtual ICollection<BattleResult> BattleResults { get; set; } = new List<BattleResult>();

    public virtual ICollection<BattleStatus> BattleStatususer1s { get; set; } = new List<BattleStatus>();

    public virtual ICollection<BattleStatus> BattleStatususer2s { get; set; } = new List<BattleStatus>();

    public virtual ICollection<EmailTemplete> EmailTempletecreated_byNavigations { get; set; } = new List<EmailTemplete>();

    public virtual ICollection<EmailTemplete> EmailTempletemodified_byNavigations { get; set; } = new List<EmailTemplete>();

    public virtual ICollection<GlobalPayment> GlobalPaymentcreated_byNavigations { get; set; } = new List<GlobalPayment>();

    public virtual ICollection<GlobalPayment> GlobalPaymentmodified_byNavigations { get; set; } = new List<GlobalPayment>();

    public virtual ICollection<GlobalPayment> GlobalPaymentpaid_by_users { get; set; } = new List<GlobalPayment>();

    public virtual ICollection<GradeForQuizResult> GradeForQuizResultcreated_byNavigations { get; set; } = new List<GradeForQuizResult>();

    public virtual ICollection<GradeForQuizResult> GradeForQuizResultmodified_byNavigations { get; set; } = new List<GradeForQuizResult>();

    public virtual ICollection<User> Inversecreated_byNavigation { get; set; } = new List<User>();

    public virtual ICollection<User> Inversemodified_byNavigation { get; set; } = new List<User>();

    public virtual ICollection<Notification> Notificationcreated_byNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> Notificationmodified_byNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentPlatform> PaymentPlatformcreated_byNavigations { get; set; } = new List<PaymentPlatform>();

    public virtual ICollection<PaymentPlatform> PaymentPlatformmodified_byNavigations { get; set; } = new List<PaymentPlatform>();

    public virtual ICollection<PaymentType> PaymentTypecreated_byNavigations { get; set; } = new List<PaymentType>();

    public virtual ICollection<PaymentType> PaymentTypemodified_byNavigations { get; set; } = new List<PaymentType>();

    public virtual ICollection<PlatformConfiguration> PlatformConfigurations { get; set; } = new List<PlatformConfiguration>();

    public virtual ICollection<QueOptionsAn> QueOptionsAncreated_byNavigations { get; set; } = new List<QueOptionsAn>();

    public virtual ICollection<QueOptionsAn> QueOptionsAnmodified_byNavigations { get; set; } = new List<QueOptionsAn>();

    public virtual ICollection<QuestionDifficulty> QuestionDifficultycreated_byNavigations { get; set; } = new List<QuestionDifficulty>();

    public virtual ICollection<QuestionDifficulty> QuestionDifficultymodified_byNavigations { get; set; } = new List<QuestionDifficulty>();

    public virtual ICollection<QuestionIssueReport> QuestionIssueReports { get; set; } = new List<QuestionIssueReport>();

    public virtual ICollection<QuizAttempted> QuizAttempteds { get; set; } = new List<QuizAttempted>();

    public virtual ICollection<QuizCategory> QuizCategorycreated_byNavigations { get; set; } = new List<QuizCategory>();

    public virtual ICollection<QuizCategory> QuizCategorymodified_byNavigations { get; set; } = new List<QuizCategory>();

    public virtual ICollection<QuizDifficulty> QuizDifficultycreated_byNavigations { get; set; } = new List<QuizDifficulty>();

    public virtual ICollection<QuizDifficulty> QuizDifficultymodified_byNavigations { get; set; } = new List<QuizDifficulty>();

    public virtual ICollection<QuizPurchased> QuizPurchaseds { get; set; } = new List<QuizPurchased>();

    public virtual ICollection<QuizRating> QuizRatings { get; set; } = new List<QuizRating>();

    public virtual ICollection<QuizTagMapping> QuizTagMappingcreated_byNavigations { get; set; } = new List<QuizTagMapping>();

    public virtual ICollection<QuizTagMapping> QuizTagMappingmodified_byNavigations { get; set; } = new List<QuizTagMapping>();

    public virtual ICollection<QuizTag> QuizTagcreated_byNavigations { get; set; } = new List<QuizTag>();

    public virtual ICollection<QuizTag> QuizTagmodified_byNavigations { get; set; } = new List<QuizTag>();

    public virtual ICollection<Quiz> Quizcreated_byNavigations { get; set; } = new List<Quiz>();

    public virtual ICollection<Quiz> Quizmodified_byNavigations { get; set; } = new List<Quiz>();

    public virtual ICollection<UserBadgesEarned> UserBadgesEarneds { get; set; } = new List<UserBadgesEarned>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizcreated_byNavigations { get; set; } = new List<UserFavoriteQuiz>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizmodified_byNavigations { get; set; } = new List<UserFavoriteQuiz>();

    public virtual ICollection<UserFavoriteQuiz> UserFavoriteQuizusers { get; set; } = new List<UserFavoriteQuiz>();

    public virtual ICollection<UserNotification> UserNotificationcreated_byNavigations { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserNotification> UserNotificationmodified_byNavigations { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserNotification> UserNotificationusers { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserPerformanceDetail> UserPerformanceDetails { get; set; } = new List<UserPerformanceDetail>();

    public virtual User? created_byNavigation { get; set; }

    public virtual User? modified_byNavigation { get; set; }

    public virtual UserRole role { get; set; } = null!;
}
