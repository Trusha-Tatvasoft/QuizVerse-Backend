using QuizVerse.Application.Core.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class CommonService : ICommonService
    {
        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}