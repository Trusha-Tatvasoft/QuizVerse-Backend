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
        Player = 2
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

    public enum BattleType
    {
        Permanent = 1,
        TimeLimited = 2
    }

    public enum EmailTemplateType
    {
        AccountSuspension = 1,
        BattleRequest = 2,
        QuizInvitation = 3,
        ResetPassword = 4,
        WelComeEmail = 5,
    }
}