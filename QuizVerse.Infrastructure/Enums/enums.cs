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
}