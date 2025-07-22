using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Domain.Entities;

namespace QuizVerse.Domain.Data;

public partial class QuizVerseDbContext : DbContext
{
    public QuizVerseDbContext(DbContextOptions<QuizVerseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Badge> Badges { get; set; }

    public virtual DbSet<BadgeConditionsMapping> BadgeConditionsMappings { get; set; }

    public virtual DbSet<BaseQuestion> BaseQuestions { get; set; }

    public virtual DbSet<BattleList> BattleLists { get; set; }

    public virtual DbSet<BattleQuesDifficultyMap> BattleQuesDifficultyMaps { get; set; }

    public virtual DbSet<BattleRequest> BattleRequests { get; set; }

    public virtual DbSet<BattleResult> BattleResults { get; set; }

    public virtual DbSet<BattleStatus> BattleStatuses { get; set; }

    public virtual DbSet<EmailTemplete> EmailTempletes { get; set; }

    public virtual DbSet<GlobalPayment> GlobalPayments { get; set; }

    public virtual DbSet<GradeForQuizResult> GradeForQuizResults { get; set; }

    public virtual DbSet<LevelByExp> LevelByExps { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PaymentPlatform> PaymentPlatforms { get; set; }

    public virtual DbSet<PaymentType> PaymentTypes { get; set; }

    public virtual DbSet<PlatformConfiguration> PlatformConfigurations { get; set; }

    public virtual DbSet<QueOptionsAn> QueOptionsAns { get; set; }

    public virtual DbSet<QuestionDifficulty> QuestionDifficulties { get; set; }

    public virtual DbSet<QuestionIssueReport> QuestionIssueReports { get; set; }

    public virtual DbSet<QuestionType> QuestionTypes { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<QuizAttempted> QuizAttempteds { get; set; }

    public virtual DbSet<QuizCategory> QuizCategories { get; set; }

    public virtual DbSet<QuizDifficulty> QuizDifficulties { get; set; }

    public virtual DbSet<QuizPurchased> QuizPurchaseds { get; set; }

    public virtual DbSet<QuizRating> QuizRatings { get; set; }

    public virtual DbSet<QuizTag> QuizTags { get; set; }

    public virtual DbSet<QuizTagMapping> QuizTagMappings { get; set; }

    public virtual DbSet<QuizToBaseQuestionMap> QuizToBaseQuestionMaps { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBadgesEarned> UserBadgesEarneds { get; set; }

    public virtual DbSet<UserFavoriteQuiz> UserFavoriteQuizzes { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<UserPerformanceDetail> UserPerformanceDetails { get; set; }

    public virtual DbSet<UserRankByLevel> UserRankByLevels { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.id).HasName("Badges_pkey");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.description).HasMaxLength(300);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.name).HasMaxLength(100);

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.Badgecreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Badges_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.Badgemodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("Badges_modified_by_fkey");
        });

        modelBuilder.Entity<BadgeConditionsMapping>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BadgeConditionsMapping_pkey");

            entity.ToTable("BadgeConditionsMapping");

            entity.Property(e => e.condition_type).HasDefaultValue(1);
            entity.Property(e => e.condition_value)
                .HasDefaultValueSql("'0'::character varying")
                .HasColumnType("character varying");

            entity.HasOne(d => d.badge).WithMany(p => p.BadgeConditionsMappings)
                .HasForeignKey(d => d.badge_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BadgeConditionsMapping_badge_id_fkey");
        });

        modelBuilder.Entity<BaseQuestion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BaseQuestions_pkey");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.que_text).HasColumnType("character varying");

            entity.HasOne(d => d.category).WithMany(p => p.BaseQuestions)
                .HasForeignKey(d => d.category_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_category_id_fkey");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.BaseQuestioncreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.BaseQuestionmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("BaseQuestions_modified_by_fkey");

            entity.HasOne(d => d.que_difficulty).WithMany(p => p.BaseQuestions)
                .HasForeignKey(d => d.que_difficulty_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_que_difficulty_id_fkey");

            entity.HasOne(d => d.que_type).WithMany(p => p.BaseQuestions)
                .HasForeignKey(d => d.que_type_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_que_type_id_fkey");
        });

        modelBuilder.Entity<BattleList>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BattleList_pkey");

            entity.ToTable("BattleList");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.BattleListcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleList_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.BattleListmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("BattleList_modified_by_fkey");

            entity.HasOne(d => d.quiz).WithMany(p => p.BattleLists)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleList_quiz_id_fkey");
        });

        modelBuilder.Entity<BattleQuesDifficultyMap>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BattleQuesDifficultyMap_pkey");

            entity.ToTable("BattleQuesDifficultyMap");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.battle).WithMany(p => p.BattleQuesDifficultyMaps)
                .HasForeignKey(d => d.battle_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleQuesDifficultyMap_battle_id_fkey");

            entity.HasOne(d => d.que_difficulty).WithMany(p => p.BattleQuesDifficultyMaps)
                .HasForeignKey(d => d.que_difficulty_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleQuesDifficultyMap_que_difficulty_id_fkey");
        });

        modelBuilder.Entity<BattleRequest>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BattleRequest_pkey");

            entity.ToTable("BattleRequest");

            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.sending_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.status).HasDefaultValue(3);

            entity.HasOne(d => d.receiver).WithMany(p => p.BattleRequestreceivers)
                .HasForeignKey(d => d.receiver_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleRequest_receiver_id_fkey");

            entity.HasOne(d => d.sender).WithMany(p => p.BattleRequestsenders)
                .HasForeignKey(d => d.sender_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleRequest_sender_id_fkey");
        });

        modelBuilder.Entity<BattleResult>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BattleResult_pkey");

            entity.ToTable("BattleResult");

            entity.Property(e => e.looser_gained_xp).HasDefaultValue(0);
            entity.Property(e => e.winner_gained_xp).HasDefaultValue(0);

            entity.HasOne(d => d.battle_statusNavigation).WithMany(p => p.BattleResults)
                .HasForeignKey(d => d.battle_status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleResult_battle_status_fkey");

            entity.HasOne(d => d.winner).WithMany(p => p.BattleResults)
                .HasForeignKey(d => d.winner_id)
                .HasConstraintName("BattleResult_winner_id_fkey");
        });

        modelBuilder.Entity<BattleStatus>(entity =>
        {
            entity.HasKey(e => e.id).HasName("BattleStatus_pkey");

            entity.ToTable("BattleStatus");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.battle).WithMany(p => p.BattleStatuses)
                .HasForeignKey(d => d.battle_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleStatus_battle_id_fkey");

            entity.HasOne(d => d.user1).WithMany(p => p.BattleStatususer1s)
                .HasForeignKey(d => d.user1_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleStatus_user1_id_fkey");

            entity.HasOne(d => d.user2).WithMany(p => p.BattleStatususer2s)
                .HasForeignKey(d => d.user2_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleStatus_user2_id_fkey");
        });

        modelBuilder.Entity<EmailTemplete>(entity =>
        {
            entity.HasKey(e => e.id).HasName("EmailTemplete_pkey");

            entity.ToTable("EmailTemplete");

            entity.Property(e => e.body).HasColumnType("character varying");
            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.status).HasDefaultValue(true);
            entity.Property(e => e.subject).HasColumnType("character varying");
            entity.Property(e => e.title).HasMaxLength(255);

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.EmailTempletecreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EmailTemplete_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.EmailTempletemodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("EmailTemplete_modified_by_fkey");
        });

        modelBuilder.Entity<GlobalPayment>(entity =>
        {
            entity.HasKey(e => e.id).HasName("GlobalPayment_pkey");

            entity.ToTable("GlobalPayment");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.payment_id).HasColumnType("character varying");
            entity.Property(e => e.payment_status).HasDefaultValue(1);
            entity.Property(e => e.received_by).HasColumnType("character varying");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.GlobalPaymentcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.GlobalPaymentmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("GlobalPayment_modified_by_fkey");

            entity.HasOne(d => d.paid_by_user).WithMany(p => p.GlobalPaymentpaid_by_users)
                .HasForeignKey(d => d.paid_by_user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_paid_by_user_id_fkey");

            entity.HasOne(d => d.payment_platform).WithMany(p => p.GlobalPayments)
                .HasForeignKey(d => d.payment_platform_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_payment_platform_id_fkey");

            entity.HasOne(d => d.payment_type).WithMany(p => p.GlobalPayments)
                .HasForeignKey(d => d.payment_type_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_payment_type_id_fkey");
        });

        modelBuilder.Entity<GradeForQuizResult>(entity =>
        {
            entity.HasKey(e => e.id).HasName("GradeForQuizResult_pkey");

            entity.ToTable("GradeForQuizResult");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.grade).HasColumnType("character varying");
            entity.Property(e => e.max_percentage).HasPrecision(5, 2);
            entity.Property(e => e.min_percentage).HasPrecision(5, 2);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.GradeForQuizResultcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GradeForQuizResult_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.GradeForQuizResultmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("GradeForQuizResult_modified_by_fkey");
        });

        modelBuilder.Entity<LevelByExp>(entity =>
        {
            entity.HasKey(e => e.id).HasName("LevelByExp_pkey");

            entity.ToTable("LevelByExp");

            entity.HasIndex(e => e.level_order, "LevelByExp_level_order_key").IsUnique();

            entity.Property(e => e.maximum_exp).HasDefaultValue(0);
            entity.Property(e => e.minimum_exp).HasDefaultValue(0);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.id).HasName("Notifications_pkey");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.is_global).HasDefaultValue(false);
            entity.Property(e => e.is_urgent).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.notification_message).HasColumnType("character varying");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.Notificationcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notifications_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.Notificationmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("Notifications_modified_by_fkey");
        });

        modelBuilder.Entity<PaymentPlatform>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PaymentPlatform_pkey");

            entity.ToTable("PaymentPlatform");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.name).HasColumnType("character varying");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.PaymentPlatformcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("PaymentPlatform_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.PaymentPlatformmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("PaymentPlatform_modified_by_fkey");
        });

        modelBuilder.Entity<PaymentType>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PaymentType_pkey");

            entity.ToTable("PaymentType");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.PaymentTypecreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("PaymentType_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.PaymentTypemodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("PaymentType_modified_by_fkey");
        });

        modelBuilder.Entity<PlatformConfiguration>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PlatformConfiguration_pkey");

            entity.ToTable("PlatformConfiguration");

            entity.Property(e => e.configuration_name).HasColumnType("character varying");
            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.values).HasColumnType("json");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.PlatformConfigurations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("PlatformConfiguration_modified_by_fkey");
        });

        modelBuilder.Entity<QueOptionsAn>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QueOptionsAns_pkey");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.key).HasColumnType("character varying");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.value).HasColumnType("character varying");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.QueOptionsAncreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QueOptionsAns_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.QueOptionsAnmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("QueOptionsAns_modified_by_fkey");

            entity.HasOne(d => d.question).WithMany(p => p.QueOptionsAns)
                .HasForeignKey(d => d.question_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QueOptionsAns_question_id_fkey");
        });

        modelBuilder.Entity<QuestionDifficulty>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuestionDifficulty_pkey");

            entity.ToTable("QuestionDifficulty");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.name).HasColumnType("character varying");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.QuestionDifficultycreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionDifficulty_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.QuestionDifficultymodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("QuestionDifficulty_modified_by_fkey");
        });

        modelBuilder.Entity<QuestionIssueReport>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuestionIssueReports_pkey");

            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.is_resolved).HasDefaultValue(false);

            entity.HasOne(d => d.question).WithMany(p => p.QuestionIssueReports)
                .HasForeignKey(d => d.question_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_question_id_fkey");

            entity.HasOne(d => d.quiz).WithMany(p => p.QuestionIssueReports)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_quiz_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.QuestionIssueReports)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_user_id_fkey");
        });

        modelBuilder.Entity<QuestionType>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuestionType_pkey");

            entity.ToTable("QuestionType");

            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.type_name).HasColumnType("character varying");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.id).HasName("Quiz_pkey");

            entity.ToTable("Quiz");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.is_featured).HasDefaultValue(false);
            entity.Property(e => e.is_paid).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.name).HasMaxLength(255);
            entity.Property(e => e.no_of_person_attempted).HasDefaultValue(0);
            entity.Property(e => e.price).HasPrecision(10, 2);
            entity.Property(e => e.rating).HasPrecision(2, 1);
            entity.Property(e => e.total_xp).HasDefaultValue(0);

            entity.HasOne(d => d.category).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.category_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Quiz_category_id_fkey");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.Quizcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Quiz_created_by_fkey");

            entity.HasOne(d => d.difficulty_level).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.difficulty_level_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Quiz_difficulty_level_id_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.Quizmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("Quiz_modified_by_fkey");
        });

        modelBuilder.Entity<QuizAttempted>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizAttempted_pkey");

            entity.ToTable("QuizAttempted");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.gradeNavigation).WithMany(p => p.QuizAttempteds)
                .HasForeignKey(d => d.grade)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizAttempted_grade_fkey");

            entity.HasOne(d => d.quiz).WithMany(p => p.QuizAttempteds)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizAttempted_quiz_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.QuizAttempteds)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizAttempted_user_id_fkey");
        });

        modelBuilder.Entity<QuizCategory>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizCategory_pkey");

            entity.ToTable("QuizCategory");

            entity.Property(e => e.category_name).HasMaxLength(255);
            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.icon).HasColumnType("character varying");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.status).HasDefaultValue(true);

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.QuizCategorycreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizCategory_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.QuizCategorymodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("QuizCategory_modified_by_fkey");
        });

        modelBuilder.Entity<QuizDifficulty>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizDifficulty_pkey");

            entity.ToTable("QuizDifficulty");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.description).HasColumnType("character varying");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.name).HasColumnType("character varying");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.QuizDifficultycreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizDifficulty_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.QuizDifficultymodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("QuizDifficulty_modified_by_fkey");
        });

        modelBuilder.Entity<QuizPurchased>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizPurchased_pkey");

            entity.ToTable("QuizPurchased");

            entity.Property(e => e.purchased_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.quiz).WithMany(p => p.QuizPurchaseds)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizPurchased_quiz_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.QuizPurchaseds)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizPurchased_user_id_fkey");
        });

        modelBuilder.Entity<QuizRating>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizRating_pkey");

            entity.ToTable("QuizRating");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.feedback).HasColumnType("character varying");

            entity.HasOne(d => d.quiz).WithMany(p => p.QuizRatings)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizRating_quiz_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.QuizRatings)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizRating_user_id_fkey");
        });

        modelBuilder.Entity<QuizTag>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizTag_pkey");

            entity.ToTable("QuizTag");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.tag_name).HasMaxLength(255);

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.QuizTagcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTag_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.QuizTagmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("QuizTag_modified_by_fkey");
        });

        modelBuilder.Entity<QuizTagMapping>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizTagMapping_pkey");

            entity.ToTable("QuizTagMapping");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.QuizTagMappingcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTagMapping_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.QuizTagMappingmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("QuizTagMapping_modified_by_fkey");

            entity.HasOne(d => d.quiz).WithMany(p => p.QuizTagMappings)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTagMapping_quiz_id_fkey");

            entity.HasOne(d => d.tag).WithMany(p => p.QuizTagMappings)
                .HasForeignKey(d => d.tag_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTagMapping_tag_id_fkey");
        });

        modelBuilder.Entity<QuizToBaseQuestionMap>(entity =>
        {
            entity.HasKey(e => e.id).HasName("QuizToBaseQuestionMap_pkey");

            entity.ToTable("QuizToBaseQuestionMap");

            entity.HasOne(d => d.que).WithMany(p => p.QuizToBaseQuestionMaps)
                .HasForeignKey(d => d.que_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizToBaseQuestionMap_que_id_fkey");

            entity.HasOne(d => d.quiz).WithMany(p => p.QuizToBaseQuestionMaps)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizToBaseQuestionMap_quiz_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.id).HasName("Users_pkey");

            entity.HasIndex(e => e.email, "Users_email_key").IsUnique();

            entity.HasIndex(e => e.user_name, "Users_user_name_key").IsUnique();

            entity.Property(e => e.bio).HasMaxLength(255);
            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.email).HasColumnType("character varying");
            entity.Property(e => e.first_time_login).HasDefaultValue(true);
            entity.Property(e => e.full_name).HasMaxLength(255);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.last_login).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.password).HasColumnType("character varying");
            entity.Property(e => e.profile_pic).HasColumnType("character varying");
            entity.Property(e => e.user_name).HasMaxLength(255);

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.Inversecreated_byNavigation)
                .HasForeignKey(d => d.created_by)
                .HasConstraintName("Users_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.Inversemodified_byNavigation)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("Users_modified_by_fkey");

            entity.HasOne(d => d.role).WithMany(p => p.Users)
                .HasForeignKey(d => d.role_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Users_role_id_fkey");
        });

        modelBuilder.Entity<UserBadgesEarned>(entity =>
        {
            entity.HasKey(e => e.id).HasName("UserBadgesEarned_pkey");

            entity.ToTable("UserBadgesEarned");

            entity.Property(e => e.date_earned).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.badge).WithMany(p => p.UserBadgesEarneds)
                .HasForeignKey(d => d.badge_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserBadgesEarned_badge_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.UserBadgesEarneds)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserBadgesEarned_user_id_fkey");
        });

        modelBuilder.Entity<UserFavoriteQuiz>(entity =>
        {
            entity.HasKey(e => e.id).HasName("UserFavoriteQuizzes_pkey");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.UserFavoriteQuizcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserFavoriteQuizzes_created_by_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.UserFavoriteQuizmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("UserFavoriteQuizzes_modified_by_fkey");

            entity.HasOne(d => d.quiz).WithMany(p => p.UserFavoriteQuizzes)
                .HasForeignKey(d => d.quiz_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserFavoriteQuizzes_quiz_id_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.UserFavoriteQuizusers)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserFavoriteQuizzes_user_id_fkey");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.id).HasName("UserNotifications_pkey");

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.is_read).HasDefaultValue(false);
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.UserNotificationcreated_byNavigations)
                .HasForeignKey(d => d.created_by)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserNotifications_created_by_fkey");

            entity.HasOne(d => d.global_notification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.global_notification_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserNotifications_global_notification_id_fkey");

            entity.HasOne(d => d.modified_byNavigation).WithMany(p => p.UserNotificationmodified_byNavigations)
                .HasForeignKey(d => d.modified_by)
                .HasConstraintName("UserNotifications_modified_by_fkey");

            entity.HasOne(d => d.user).WithMany(p => p.UserNotificationusers)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserNotifications_user_id_fkey");
        });

        modelBuilder.Entity<UserPerformanceDetail>(entity =>
        {
            entity.HasKey(e => e.id).HasName("UserPerformanceDetails_pkey");

            entity.HasIndex(e => e.new_global_rank, "UserPerformanceDetails_new_global_rank_key").IsUnique();

            entity.HasIndex(e => e.old_global_rank, "UserPerformanceDetails_old_global_rank_key").IsUnique();

            entity.Property(e => e.created_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.modified_date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.user).WithMany(p => p.UserPerformanceDetails)
                .HasForeignKey(d => d.user_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserPerformanceDetails_user_id_fkey");
        });

        modelBuilder.Entity<UserRankByLevel>(entity =>
        {
            entity.HasKey(e => e.id).HasName("UserRankByLevel_pkey");

            entity.ToTable("UserRankByLevel");

            entity.Property(e => e.rank_name).HasColumnType("character varying");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.id).HasName("UserRoles_pkey");

            entity.Property(e => e.name).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
