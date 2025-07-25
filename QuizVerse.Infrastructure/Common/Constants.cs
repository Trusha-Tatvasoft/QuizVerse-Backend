namespace QuizVerse.Infrastructure.Common
{
    public static class Constants
    {
        public const string QUIZVERSE_DEFAULT_QUOTE = "Welcome to QuizVerse!";

        public static class Messages
        {
            public const string FETCH_SUCCESS = "Data fetched successfully";
            public const string CREATE_SUCCESS = "Created successfully";
            public const string UPDATE_SUCCESS = "Updated successfully";
            public const string DELETE_SUCCESS = "Deleted successfully";
            public const string STATUS_CHANGED = "Status changed to {0} successfully.";
            public const string STATUS_REQUIRED = "Status must be provided.";

            public static class User
            {
                public const string DUPLICATE_EMAIL = "User with this email already exists.";
                public const string DUPLICATE_USERNAME = "User with this username already exists.";
                public const string NOT_FOUND = "User with ID {0} not found.";
                public const string STATUS_ALREADY_SET = "User is already {0}.";
            }
        }
    }
}
