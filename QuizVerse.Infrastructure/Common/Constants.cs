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

        public const string FETCH_DATA_MESSAGE = "Data Fetched Successfully";

        #region AdminDashboard
        public const string DASHBOARD_SUMMARY_FETCH = "Admin dashboard summary fetched successfully.";
        public const string USER_ENGAGEMENT_DATA_FETCH = "User engagement data retrieved successfully.";
        public const string REVENUE_TREND_DATA_FETCH = "Revenue trend data retrieved successfully.";
        public const string PERFORMANCE_SCORE_DATA_FETCH = "Performance score data retrieved successfully.";
        #endregion

        #region DateValidation
        public const string START_DATE_REQUIRED = "start_date is required.";
        public const string END_DATE_REQUIRED = "end_date is required.";
        public const string INVALID_START_DATE_FORMAT = "Invalid start_date format. Use yyyy-MM-dd.";
        public const string INVALID_END_DATE_FORMAT = "Invalid end_date format. Use yyyy-MM-dd.";
        public const string START_DATE_AFTER_END_DATE = "start_date cannot be after end_date.";
        #endregion
    }
}
