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
            entity.HasKey(e => e.Id).HasName("Badges_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BadgeType).HasColumnName("badge_type");
            entity.Property(e => e.CategoryType).HasColumnName("category_type");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasMaxLength(300)
                .HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Xp).HasColumnName("xp");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BadgeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Badges_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.BadgeModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("Badges_modified_by_fkey");
        });

        modelBuilder.Entity<BadgeConditionsMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BadgeConditionsMapping_pkey");

            entity.ToTable("BadgeConditionsMapping");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BadgeId).HasColumnName("badge_id");
            entity.Property(e => e.ConditionType)
                .HasDefaultValue(1)
                .HasColumnName("condition_type");
            entity.Property(e => e.ConditionValue)
                .HasDefaultValueSql("'0'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("condition_value");

            entity.HasOne(d => d.Badge).WithMany(p => p.BadgeConditionsMappings)
                .HasForeignKey(d => d.BadgeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BadgeConditionsMapping_badge_id_fkey");
        });

        modelBuilder.Entity<BaseQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BaseQuestions_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.QueDifficultyId).HasColumnName("que_difficulty_id");
            entity.Property(e => e.QueText)
                .HasColumnType("character varying")
                .HasColumnName("que_text");
            entity.Property(e => e.QueTypeId).HasColumnName("que_type_id");

            entity.HasOne(d => d.Category).WithMany(p => p.BaseQuestions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BaseQuestionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.BaseQuestionModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("BaseQuestions_modified_by_fkey");

            entity.HasOne(d => d.QueDifficulty).WithMany(p => p.BaseQuestions)
                .HasForeignKey(d => d.QueDifficultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_que_difficulty_id_fkey");

            entity.HasOne(d => d.QueType).WithMany(p => p.BaseQuestions)
                .HasForeignKey(d => d.QueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BaseQuestions_que_type_id_fkey");
        });

        modelBuilder.Entity<BattleList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BattleList_pkey");

            entity.ToTable("BattleList");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BattleListCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleList_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.BattleListModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("BattleList_modified_by_fkey");

            entity.HasOne(d => d.Quiz).WithMany(p => p.BattleLists)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleList_quiz_id_fkey");
        });

        modelBuilder.Entity<BattleQuesDifficultyMap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BattleQuesDifficultyMap_pkey");

            entity.ToTable("BattleQuesDifficultyMap");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BattleId).HasColumnName("battle_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.NoOfQues).HasColumnName("no_of_ques");
            entity.Property(e => e.QueDifficultyId).HasColumnName("que_difficulty_id");
            entity.Property(e => e.QusTime).HasColumnName("qus_time");

            entity.HasOne(d => d.Battle).WithMany(p => p.BattleQuesDifficultyMaps)
                .HasForeignKey(d => d.BattleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleQuesDifficultyMap_battle_id_fkey");

            entity.HasOne(d => d.QueDifficulty).WithMany(p => p.BattleQuesDifficultyMaps)
                .HasForeignKey(d => d.QueDifficultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleQuesDifficultyMap_que_difficulty_id_fkey");
        });

        modelBuilder.Entity<BattleRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BattleRequest_pkey");

            entity.ToTable("BattleRequest");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SendingDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("sending_date");
            entity.Property(e => e.Status)
                .HasDefaultValue(3)
                .HasColumnName("status");

            entity.HasOne(d => d.Receiver).WithMany(p => p.BattleRequestReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleRequest_receiver_id_fkey");

            entity.HasOne(d => d.Sender).WithMany(p => p.BattleRequestSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleRequest_sender_id_fkey");
        });

        modelBuilder.Entity<BattleResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BattleResult_pkey");

            entity.ToTable("BattleResult");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BattleStatus).HasColumnName("battle_status");
            entity.Property(e => e.LooserGainedXp)
                .HasDefaultValue(0)
                .HasColumnName("looser_gained_xp");
            entity.Property(e => e.User1CorrectedAns).HasColumnName("user1_corrected_ans");
            entity.Property(e => e.User1TakenTime).HasColumnName("user1_taken_time");
            entity.Property(e => e.User2CorrectedAns).HasColumnName("user2_corrected_ans");
            entity.Property(e => e.User2TakenTime).HasColumnName("user2_taken_time");
            entity.Property(e => e.WinnerGainedXp)
                .HasDefaultValue(0)
                .HasColumnName("winner_gained_xp");
            entity.Property(e => e.WinnerId).HasColumnName("winner_id");

            entity.HasOne(d => d.BattleStatusNavigation).WithMany(p => p.BattleResults)
                .HasForeignKey(d => d.BattleStatus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleResult_battle_status_fkey");

            entity.HasOne(d => d.Winner).WithMany(p => p.BattleResults)
                .HasForeignKey(d => d.WinnerId)
                .HasConstraintName("BattleResult_winner_id_fkey");
        });

        modelBuilder.Entity<BattleStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BattleStatus_pkey");

            entity.ToTable("BattleStatus");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BattleId).HasColumnName("battle_id");
            entity.Property(e => e.BattleStatus1).HasColumnName("battle_status");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.User1Id).HasColumnName("user1_id");
            entity.Property(e => e.User2Id).HasColumnName("user2_id");

            entity.HasOne(d => d.Battle).WithMany(p => p.BattleStatuses)
                .HasForeignKey(d => d.BattleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleStatus_battle_id_fkey");

            entity.HasOne(d => d.User1).WithMany(p => p.BattleStatusUser1s)
                .HasForeignKey(d => d.User1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleStatus_user1_id_fkey");

            entity.HasOne(d => d.User2).WithMany(p => p.BattleStatusUser2s)
                .HasForeignKey(d => d.User2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BattleStatus_user2_id_fkey");
        });

        modelBuilder.Entity<EmailTemplete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("EmailTemplete_pkey");

            entity.ToTable("EmailTemplete");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Body)
                .HasColumnType("character varying")
                .HasColumnName("body");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasColumnType("character varying")
                .HasColumnName("subject");
            entity.Property(e => e.TemplateType).HasColumnName("template_type");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmailTempleteCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EmailTemplete_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.EmailTempleteModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("EmailTemplete_modified_by_fkey");
        });

        modelBuilder.Entity<GlobalPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GlobalPayment_pkey");

            entity.ToTable("GlobalPayment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.PaidByUserId).HasColumnName("paid_by_user_id");
            entity.Property(e => e.PaymentAmount).HasColumnName("payment_amount");
            entity.Property(e => e.PaymentId)
                .HasColumnType("character varying")
                .HasColumnName("payment_id");
            entity.Property(e => e.PaymentPlatformId).HasColumnName("payment_platform_id");
            entity.Property(e => e.PaymentStatus)
                .HasDefaultValue(1)
                .HasColumnName("payment_status");
            entity.Property(e => e.PaymentTypeId).HasColumnName("payment_type_id");
            entity.Property(e => e.ReceivedBy)
                .HasColumnType("character varying")
                .HasColumnName("received_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.GlobalPaymentCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.GlobalPaymentModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("GlobalPayment_modified_by_fkey");

            entity.HasOne(d => d.PaidByUser).WithMany(p => p.GlobalPaymentPaidByUsers)
                .HasForeignKey(d => d.PaidByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_paid_by_user_id_fkey");

            entity.HasOne(d => d.PaymentPlatform).WithMany(p => p.GlobalPayments)
                .HasForeignKey(d => d.PaymentPlatformId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_payment_platform_id_fkey");

            entity.HasOne(d => d.PaymentType).WithMany(p => p.GlobalPayments)
                .HasForeignKey(d => d.PaymentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GlobalPayment_payment_type_id_fkey");
        });

        modelBuilder.Entity<GradeForQuizResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GradeForQuizResult_pkey");

            entity.ToTable("GradeForQuizResult");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Grade)
                .HasColumnType("character varying")
                .HasColumnName("grade");
            entity.Property(e => e.MaxPercentage)
                .HasPrecision(5, 2)
                .HasColumnName("max_percentage");
            entity.Property(e => e.MinPercentage)
                .HasPrecision(5, 2)
                .HasColumnName("min_percentage");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.GradeForQuizResultCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GradeForQuizResult_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.GradeForQuizResultModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("GradeForQuizResult_modified_by_fkey");
        });

        modelBuilder.Entity<LevelByExp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("LevelByExp_pkey");

            entity.ToTable("LevelByExp");

            entity.HasIndex(e => e.LevelOrder, "LevelByExp_level_order_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LevelOrder).HasColumnName("level_order");
            entity.Property(e => e.MaximumExp)
                .HasDefaultValue(0)
                .HasColumnName("maximum_exp");
            entity.Property(e => e.MinimumExp)
                .HasDefaultValue(0)
                .HasColumnName("minimum_exp");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Notifications_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.IsGlobal)
                .HasDefaultValue(false)
                .HasColumnName("is_global");
            entity.Property(e => e.IsUrgent)
                .HasDefaultValue(false)
                .HasColumnName("is_urgent");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.NotificationCategory).HasColumnName("notification_category");
            entity.Property(e => e.NotificationMessage)
                .HasColumnType("character varying")
                .HasColumnName("notification_message");
            entity.Property(e => e.NotificationType).HasColumnName("notification_type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notifications_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.NotificationModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("Notifications_modified_by_fkey");
        });

        modelBuilder.Entity<PaymentPlatform>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PaymentPlatform_pkey");

            entity.ToTable("PaymentPlatform");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentPlatformCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("PaymentPlatform_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.PaymentPlatformModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("PaymentPlatform_modified_by_fkey");
        });

        modelBuilder.Entity<PaymentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PaymentType_pkey");

            entity.ToTable("PaymentType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentTypeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("PaymentType_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.PaymentTypeModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("PaymentType_modified_by_fkey");
        });

        modelBuilder.Entity<PlatformConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlatformConfiguration_pkey");

            entity.ToTable("PlatformConfiguration");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConfigurationName)
                .HasColumnType("character varying")
                .HasColumnName("configuration_name");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Values)
                .HasColumnType("json")
                .HasColumnName("values");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.PlatformConfigurations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("PlatformConfiguration_modified_by_fkey");
        });

        modelBuilder.Entity<QueOptionsAn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QueOptionsAns_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Key)
                .HasColumnType("character varying")
                .HasColumnName("key");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Value)
                .HasColumnType("character varying")
                .HasColumnName("value");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QueOptionsAnCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QueOptionsAns_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QueOptionsAnModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QueOptionsAns_modified_by_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.QueOptionsAns)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QueOptionsAns_question_id_fkey");
        });

        modelBuilder.Entity<QuestionDifficulty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuestionDifficulty_pkey");

            entity.ToTable("QuestionDifficulty");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.XpGained).HasColumnName("xp_gained");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuestionDifficultyCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionDifficulty_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuestionDifficultyModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QuestionDifficulty_modified_by_fkey");
        });

        modelBuilder.Entity<QuestionIssueReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuestionIssueReports_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.IsResolved)
                .HasDefaultValue(false)
                .HasColumnName("is_resolved");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuestionIssueReportCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuestionIssueReportModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QuestionIssueReports_modified_by_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionIssueReports)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_question_id_fkey");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuestionIssueReports)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_quiz_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.QuestionIssueReportUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuestionIssueReports_user_id_fkey");
        });

        modelBuilder.Entity<QuestionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuestionType_pkey");

            entity.ToTable("QuestionType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.TypeName)
                .HasColumnType("character varying")
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Quiz_pkey");

            entity.ToTable("Quiz");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.DifficultyLevelId).HasColumnName("difficulty_level_id");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.IsFeatured)
                .HasDefaultValue(false)
                .HasColumnName("is_featured");
            entity.Property(e => e.IsPaid)
                .HasDefaultValue(false)
                .HasColumnName("is_paid");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NoOfPersonAttempted)
                .HasDefaultValue(0)
                .HasColumnName("no_of_person_attempted");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Rating)
                .HasPrecision(2, 1)
                .HasColumnName("rating");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalQuestion).HasColumnName("total_question");
            entity.Property(e => e.TotalTime).HasColumnName("total_time");
            entity.Property(e => e.TotalXp)
                .HasDefaultValue(0)
                .HasColumnName("total_xp");

            entity.HasOne(d => d.Category).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Quiz_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuizCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Quiz_created_by_fkey");

            entity.HasOne(d => d.DifficultyLevel).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.DifficultyLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Quiz_difficulty_level_id_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuizModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("Quiz_modified_by_fkey");
        });

        modelBuilder.Entity<QuizAttempted>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizAttempted_pkey");

            entity.ToTable("QuizAttempted");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CorrectedQue).HasColumnName("corrected_que");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.TimeSpent).HasColumnName("time_spent");
            entity.Property(e => e.TotalQue).HasColumnName("total_que");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.XpEarned).HasColumnName("xp_earned");

            entity.HasOne(d => d.GradeNavigation).WithMany(p => p.QuizAttempteds)
                .HasForeignKey(d => d.Grade)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizAttempted_grade_fkey");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizAttempteds)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizAttempted_quiz_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.QuizAttempteds)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizAttempted_user_id_fkey");
        });

        modelBuilder.Entity<QuizCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizCategory_pkey");

            entity.ToTable("QuizCategory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(255)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Icon)
                .HasColumnType("character varying")
                .HasColumnName("icon");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuizCategoryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizCategory_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuizCategoryModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QuizCategory_modified_by_fkey");
        });

        modelBuilder.Entity<QuizDifficulty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizDifficulty_pkey");

            entity.ToTable("QuizDifficulty");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuizDifficultyCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizDifficulty_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuizDifficultyModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QuizDifficulty_modified_by_fkey");
        });

        modelBuilder.Entity<QuizPurchased>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizPurchased_pkey");

            entity.ToTable("QuizPurchased");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PurchasedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("purchased_date");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizPurchaseds)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizPurchased_quiz_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.QuizPurchaseds)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizPurchased_user_id_fkey");
        });

        modelBuilder.Entity<QuizRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizRating_pkey");

            entity.ToTable("QuizRating");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Feedback)
                .HasColumnType("character varying")
                .HasColumnName("feedback");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.QuizRating1).HasColumnName("quiz_rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizRatings)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizRating_quiz_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.QuizRatings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizRating_user_id_fkey");
        });

        modelBuilder.Entity<QuizTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizTag_pkey");

            entity.ToTable("QuizTag");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.TagName)
                .HasMaxLength(255)
                .HasColumnName("tag_name");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuizTagCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTag_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuizTagModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QuizTag_modified_by_fkey");
        });

        modelBuilder.Entity<QuizTagMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizTagMapping_pkey");

            entity.ToTable("QuizTagMapping");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.QuizTagMappingCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTagMapping_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.QuizTagMappingModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("QuizTagMapping_modified_by_fkey");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizTagMappings)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTagMapping_quiz_id_fkey");

            entity.HasOne(d => d.Tag).WithMany(p => p.QuizTagMappings)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizTagMapping_tag_id_fkey");
        });

        modelBuilder.Entity<QuizToBaseQuestionMap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("QuizToBaseQuestionMap_pkey");

            entity.ToTable("QuizToBaseQuestionMap");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QueId).HasColumnName("que_id");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");

            entity.HasOne(d => d.Que).WithMany(p => p.QuizToBaseQuestionMaps)
                .HasForeignKey(d => d.QueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizToBaseQuestionMap_que_id_fkey");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizToBaseQuestionMaps)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("QuizToBaseQuestionMap_quiz_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.HasIndex(e => e.Email, "Users_email_key").IsUnique();

            entity.HasIndex(e => e.UserName, "Users_user_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Bio)
                .HasMaxLength(255)
                .HasColumnName("bio");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.FirstTimeLogin)
                .HasDefaultValue(true)
                .HasColumnName("first_time_login");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.LastLogin)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("last_login");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.ProfilePic)
                .HasColumnType("character varying")
                .HasColumnName("profile_pic");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .HasColumnName("user_name");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("Users_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.InverseModifiedByNavigation)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("Users_modified_by_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Users_role_id_fkey");
        });

        modelBuilder.Entity<UserBadgesEarned>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserBadgesEarned_pkey");

            entity.ToTable("UserBadgesEarned");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BadgeId).HasColumnName("badge_id");
            entity.Property(e => e.DateEarned)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("date_earned");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Badge).WithMany(p => p.UserBadgesEarneds)
                .HasForeignKey(d => d.BadgeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserBadgesEarned_badge_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserBadgesEarneds)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserBadgesEarned_user_id_fkey");
        });

        modelBuilder.Entity<UserFavoriteQuiz>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserFavoriteQuizzes_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserFavoriteQuizCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserFavoriteQuizzes_created_by_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.UserFavoriteQuizModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("UserFavoriteQuizzes_modified_by_fkey");

            entity.HasOne(d => d.Quiz).WithMany(p => p.UserFavoriteQuizzes)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserFavoriteQuizzes_quiz_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavoriteQuizUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserFavoriteQuizzes_user_id_fkey");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserNotifications_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.GlobalNotificationId).HasColumnName("global_notification_id");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserNotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserNotifications_created_by_fkey");

            entity.HasOne(d => d.GlobalNotification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.GlobalNotificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserNotifications_global_notification_id_fkey");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.UserNotificationModifiedByNavigations)
                .HasForeignKey(d => d.ModifiedBy)
                .HasConstraintName("UserNotifications_modified_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotificationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserNotifications_user_id_fkey");
        });

        modelBuilder.Entity<UserPerformanceDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserPerformanceDetails_pkey");

            entity.HasIndex(e => e.NewGlobalRank, "UserPerformanceDetails_new_global_rank_key").IsUnique();

            entity.HasIndex(e => e.OldGlobalRank, "UserPerformanceDetails_old_global_rank_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.CurrentLevel).HasColumnName("current_level");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
            entity.Property(e => e.HighestStreak).HasColumnName("highest_streak");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_date");
            entity.Property(e => e.NewGlobalRank).HasColumnName("new_global_rank");
            entity.Property(e => e.OldGlobalRank).HasColumnName("old_global_rank");
            entity.Property(e => e.TotalXp).HasColumnName("total_xp");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserPerformanceDetails)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserPerformanceDetails_user_id_fkey");
        });

        modelBuilder.Entity<UserRankByLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserRankByLevel_pkey");

            entity.ToTable("UserRankByLevel");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaximumLevel).HasColumnName("maximum_level");
            entity.Property(e => e.MinimumLevel).HasColumnName("minimum_level");
            entity.Property(e => e.RankName)
                .HasColumnType("character varying")
                .HasColumnName("rank_name");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserRoles_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
