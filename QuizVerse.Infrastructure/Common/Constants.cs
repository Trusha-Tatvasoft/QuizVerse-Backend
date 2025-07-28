using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizVerse.Infrastructure.Common
{
    public class Constants
    {
        public static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public const string QUIZVERSE_DEFAULT_QUOTE = "Welcome to QuizVerse!";

        public const string FETCH_DATA_MESSAGE = "Data Fetched Successfully.";

        public const string INVALID_DATA_MESSAGE = "Invalid Data.";

        public const string USER_NOT_FOUND_MESSAGE = "User Not Found.";

        public const string INACTIVE_USER_MESSAGE = "User is not active. Please ask Admin to Activate your Account.";

        public const string INVALID_PASSWORD_MESSAGE = "Invalid Password.";

        public const string FAILED_TOKEN_GENERATION_MESSAGE = "Failed to generate session tokens.";

        public const string REFRESH_TOKEN_REQUIRED_MESSAGE = "Refresh token is required.";

        public const string EXPIRED_LOGIN_SESSION_MESSAGE = "Your Login Session has expired. Please login again.";

        public const string INVALID_LOGIN_CREDENTIALS_MESSAGE = "Invalid Login Credentials.";

        public const string INVALID_USER_ID_MESSAGE = "Invalid UserId.";

        public const string NULL_MODIFIED_DATE_MESSAGE = "Modified Date is null.";


        public const string INVALID_TOKEN_FORMAT_MESSAGE = "Invalid Token Format.";

        public const string EXPIRED_TOKEN_MESSAGE = "Token has expired.";

        public const string EMPTY_TOKEN_MESSAGE = "Token must not be null or empty.";

        public const string JWT_KEY_ERROR_MESSAGE = "JWT Key is not configured.";

        public const string USER_SUSPENDED_MESSAGE = "You have been suspended. Remaining suspension time: {0} days and {1} hours.";

        public const string USER_LOGIN_SUCCESS_MESSAGE = "Logged in successfully.";

        public const string VALIDATE_AND_REGENERATE_REFERESH_TOKEN_SUCCESS_MESSAGE = "Tokens regenerated successfully.";
    }
}
