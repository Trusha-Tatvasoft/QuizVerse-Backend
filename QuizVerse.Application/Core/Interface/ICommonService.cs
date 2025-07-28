namespace QuizVerse.Application.Core.Interface
{
    public interface ICommonService
    {
        bool VerifyPassword(string password, string hashedPassword);
    }
}
