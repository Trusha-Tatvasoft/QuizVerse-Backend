using QuizVerse.Application.Core.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class CustomService : ICustomService
    {
        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}