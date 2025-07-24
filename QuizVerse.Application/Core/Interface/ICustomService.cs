namespace QuizVerse.Application.Core.Interface
{
    public interface ICustomService
    {
        bool VerifyPassword(string password, string hashedPassword);
    }
}
