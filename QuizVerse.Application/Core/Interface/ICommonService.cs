namespace QuizVerse.Application.Core.Interface
{
    public interface ICommonService
    {
        string Hash(string password);
        bool VerifyPassword(string password, string hashedPassword);
        DateTime ToDate(string dateString);
    }
}
