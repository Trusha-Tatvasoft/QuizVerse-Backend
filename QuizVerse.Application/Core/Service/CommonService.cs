using System.Globalization;
using QuizVerse.Application.Core.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class CommonService : ICommonService
    {
        #region PasswordHash
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        #endregion PasswordHash

        #region DateParsing
        public DateTime ToDate(string dateString)
        {
            return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
        }
        #endregion DateParsing

    }
}