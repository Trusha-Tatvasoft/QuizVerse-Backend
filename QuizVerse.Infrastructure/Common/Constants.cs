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
    }
}
