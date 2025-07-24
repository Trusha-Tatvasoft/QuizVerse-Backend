namespace QuizVerse.Application.Core.Interface
{
    public class CustomService : ICustomService
    {
        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}