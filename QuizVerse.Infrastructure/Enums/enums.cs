namespace QuizVerse.Infrastructure.Enums
{
    public enum UserStatus
    {
        Active = 1,
        Inactive = 2,
        Suspended = 3,
    }

    public enum UserRoles
    {
        Admin = 1,
        Player = 2,
        SuperAdmin = 3
    }

    public enum UserActionType
    {
        Delete = 1,
        ChangeStatus = 2
    }

    public enum QuizStatus
    {
        Active = 1,
        Draft = 2,
        Inactive = 3
    }

    public enum DropDownType
    {
        QuizCategory = 1,
        QuizDifficulty = 2,
        QuizTag = 3,
        QuestionDifficulty = 4,
        QuestionType = 5
    }

    public enum QuizCategoryStatus
    {
        Active = 1,
        Inactive = 0
    }

    public enum QuizCategoryActionType
    {
        Delete = 1,
        ChangeStatus = 2
    }

    public enum BattleStatus
    {
        Completed = 1,
        Draw = 2,
        Running = 3
    }

    public enum BattleCreationStatus
    {
        Active = 1,
        Completed = 2
    }

    public enum BattleType
    {
        Permanent = 1,
        TimeLimited = 2
    }

    public enum QuizType
    {
        Normal = 1,
        Battle = 2,
        Tournament = 3
    }

    public enum EmailTemplateType
    {
        AccountSuspension = 1,
        BattleRequest = 2,
        EmailVerification = 3,
        QuizInvitation = 4,
        ResetPassword = 5,
        WelComeEmail = 6,
        NewUser = 7
    }

    public enum BattleRequestStatus
    {
        Accepted = 1,
        Rejected = 2,
        Pending = 3,
        Cancelled = 4
    }

    public enum BadgeType
    {
        Bronze = 1,
        Gold = 2,
        Platinum = 3,
        Silver = 4
    }

    public enum EmailTemplateActionType
    {
        Delete = 1,
        ChangeStatus = 2
    }

    public enum BrowseQuizzesSorting
    {
        MostPopular = 1,
        HighestRated = 2,
        Newest = 3,
        PriceLowToHigh = 4,
    }

    public enum BrowseQuizzesFilterByType
    {
        Featured = 1,
        Free = 2,
        Premium = 3
    }

    public enum BattleTimeFilterType
    {
        Last2Days = 1,
        Last7Days = 2,
        CurrentMonth = 3,
        LastQuarter = 4,
        CurrentYear = 5,
        LastYear = 6
    }

    public enum BattleFilterType
    {
        Draw = 1,
        Lost = 2,
        Won = 3
    }

    public enum QuestionOrQuizIssueReportSeverity
    {
        High = 1,
        Low = 2,
        Medium = 3,
        UnderProcessing = 4
    }
 
    public enum QuestionOrQuizIssueReportStatus
    {
        Accepted = 1,
        Ignore= 2,
        Pending = 3,
        UnderReview = 4
    }
    public enum QuizRatingStatus
    {
        Accepted = 1,
        Pending = 2,
        Rejected = 3,
        UnderProcessing = 4,
        UnderReview = 5
    }

    public enum ReportType
    {
        QuizRatingFeedback = 1,
        QuestionIssueReport = 2,
        QuizIssueReport = 3,
    }

    public enum AiModelName
    {
        Gemini2Point5FlashLite = 1,
        Gemini2Point5Flash = 2,
        Gemini2Point0FlashLite = 3,
        Gemini2Point0Flash = 4,
        Gemini2Point5Pro = 5,
        Gemini2Point0FlashExp = 6,
        Llama3Point18BInstant = 7,
        Llama3Point370BVersatile = 8,
        GroqCompound = 9,
        MoonshotAiKimiK2Instruct = 10,
        OpenAiGptOss20B = 11
    }
}