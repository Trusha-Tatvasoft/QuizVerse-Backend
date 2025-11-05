using System.Net.Security;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.Common;

public static class Constants
{
    #region Authorize attribute Roles
    public static class RoleGroups
    {
        public const string Admins = nameof(UserRoles.Admin) + "," + nameof(UserRoles.SuperAdmin);
        public const string AllUsers = nameof(UserRoles.Admin) + "," + nameof(UserRoles.SuperAdmin) + "," + nameof(UserRoles.Player);
    }

    #endregion
    #region General Messages
    public const string PLATFORM_NAME = "QuizVerse";
    public const string QUIZVERSE_DEFAULT_QUOTE = "Welcome to QuizVerse!";
    public const string FETCH_DATA_MESSAGE = "Data Fetched Successfully.";
    public const string INVALID_DATA_MESSAGE = "Invalid Data.";
    public const string VALID_DATA = "Valid Data.";
    public const string NULL_MODIFIED_DATE_MESSAGE = "Modified Date is null.";
    public const string INVALID_USER_ID_MESSAGE = "Invalid UserId.";
    public const string INVALID_STATUS_MESSAGE = "Invalid status. Valid values: 1 (Active), 2 (Inactive), 3 (Suspend)";
    public const string INVALID_ROLE_MESSAGE = "Invalid role. Valid values: 1 (Admin), 2 (Player)";
    public const string INVALID_SEVERITY_MESSAGE = "Invalid Severity Values: 1(high), 2(low), 3(medium) 4(UnderProcessing)";
    public const string INVALID_QUIZ = "Invalid Quiz.";
    public const string NO_DATA_FOUND = "No data found.";
    public const string UNAUTHORIZED_USER = "User is not authorized.";
    public const string SENT_SUCCESS = "OTP Sent Successfully";
    #endregion

    #region Auth Messages
    public const string INACTIVE_USER_MESSAGE = "User is not active. Please ask Admin to Activate your Account.";
    public const string INVALID_PASSWORD_MESSAGE = "Invalid Password.";
    public const string INVALID_LOGIN_CREDENTIALS_MESSAGE = "Invalid Login Credentials.";
    public const string USER_LOGIN_SUCCESS_MESSAGE = "Logged in successfully.";
    public const string FAILED_TOKEN_GENERATION_MESSAGE = "Failed to generate session tokens.";
    public const string REFRESH_TOKEN_REQUIRED_MESSAGE = "Refresh token is required.";
    public const string EXPIRED_LOGIN_SESSION_MESSAGE = "Your Login Session has expired. Please login again.";
    public const string VALIDATE_AND_REGENERATE_REFERESH_TOKEN_SUCCESS_MESSAGE = "Tokens regenerated successfully.";
    public const string ACCESS_TOKEN_EXPIRYTIME_NOT_CONFIGURED_MESSAGE = "AccessTokenExpiryMinutes is not configured.";
    public const string REFRESH_TOKEN_EXPIRYTIME_NOT_CONFIGURED_MESSAGE = "RefreshTokenExpiryDays is not configured.";
    public const string USER_NOT_FOUND_MESSAGE = "User Not Found";
    public const string USERNAME_DOES_NOT_EXIST = "User with this username doesn't exist";
    public const string USERNAME_AVAILABILITY_VERIFIED = "Username availability checked successfully.";
    public const string USER_SUSPENDED_MESSAGE = "You have been suspended. Remaining suspension time: {0} days and {1} hours.";
    public const string USER_NOT_AUTHENTICATED_MESSAGE = "User is not authenticated.";
    #endregion

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

    #region Token Messages
    public const string INVALID_TOKEN_FORMAT_MESSAGE = "Invalid Token Format.";
    public const string EXPIRED_TOKEN_MESSAGE = "Token has expired.";
    public const string EMPTY_TOKEN_MESSAGE = "Token must not be null or empty.";
    public const string JWT_KEY_ERROR_MESSAGE = "JWT Key is not configured.";
    #endregion

    #region User Messages
    public const string INVALID_USER_ID = "The provided user ID {0} is invalid.";
    public const string USER_NOT_FOUND = "User with ID {0} not found.";
    public const string DUPLICATE_EMAIL = "User with this email already exists.";
    public const string DUPLICATE_USERNAME = "User with this username already exists.";
    public const string STATUS_ALREADY_SET = "User is already {0}.";
    public const string STATUS_REQUIRED = "Status must be provided.";
    public const string USER_ALREADY_DELETED = "User {0} is already deleted.";
    public const string PASSWORD_REQUIRED_FOR_NEW_USER = "Password is required for new users.";
    public const string USER_DELETED_SUCCESS = "User {0} deleted successfully.";
    public const string USER_STATUS_CHANGED_SUCCESS = "User {0} status changed to {1}.";
    public const string USER_DATA_NULL = "No data available to export.";
    public const string NO_EMAIL_CHANGE = "Email can't be changed";
    public const string USER_CREATE_SUCCESS = "User created successfully.";
    public const string PROFILE_UPDATED_SUCCESSFULLY = "Profile Updated successfully";
    public const string EMAIL_ALREADY_IN_USE_DIFFERENT_ROLE = "This email cannot be reused by you";
    public const string NOT_HAVE_PERMISSION = "You do not have permission to perform this action.";
    public const string CANNOT_MODIFY_SELF = "You cannot modify your own account.";
    #endregion

    #region Email Messages
    public const string EMAIL_PATH_NOT_CONFIGURED = "Email template path is not configured.";
    public const string EMAIL_PLACEHOLDER_MISSING = "Missing required placeholder: {0}";
    public const string EMAIL_SENT_SUCCESS = "Email successfully sent to {0}.";
    public const string USER_REGISTERED_AND_EMAIL_SENT = "User registered successfully. A welcome email has been sent.";
    public const string USER_REGISTERED_BUT_EMAIL_NOT_SENT = "User registered successfully, but email could not be sent.";
    public const string EMAIL_NOT_SENT = "Email not sent.";
    public const string SMTP_CONFIG_MISSING = "SMTP configuration is missing required fields.";
    public const string EMAIL_OTP_SUBJECT = "Your QuizVerse OTP Code";
    public const string EMAIL_BODY_EMPTY = "Email body is not provided.";
    #endregion

    #region CRUD Messages
    public const string FETCH_SUCCESS = "Data fetched successfully";
    public const string CREATE_SUCCESS = "Created successfully";
    public const string UPDATE_SUCCESS = "Updated successfully";
    public const string DELETE_SUCCESS = "Deleted successfully";
    public const string CAN_NOT_DELETE_BATTLE = "Battle is being played. So you cannot delete it.";
    public const string NO_DATA_Found = "No data found";
    #endregion

    #region Reset Password Messages
    public const string SEND_MAIL_SUCCESS_MESSAGE = "Email sent successfully.";
    public const string RESET_PASSWORD_FE_PATH = "reset-password";
    public const string VALID_RESET_PASSWORD_TOKEN = "Reset Password Token is valid.";
    public const string INVALID_RESET_PASSWORD_TOKEN = "Reset Password Token is not valid.";
    public const string PASSWORD_UPDATE_SUCCESS_MESSAGE = "Password updated successfully.";
    public const string FAILED_TO_CREATE_RESET_PASSWORD_TOKEN = "Failed to create Reset Password Token.";
    public const string RESET_PASSWORD_EMAIL_HEADING = "QuizVerse : Reset Password Link!";
    public const string ResetPasswordTemplatePath = "Templates/ResetPassword.html";
    #endregion

    #region Images Path
    public const string LOGO_PATH = "wwwroot/images/logo.png";
    #endregion

    #region File Upload Messages
    public const string INVALID_IMAGE_FILE_TYPE_MESSAGE = "Only image files are allowed: .jpg, .jpeg, .png, .gif";
    public const string IMAGE_FILE_SIZE_EXCEEDED_MESSAGE = "Maximum allowed file size is 10MB.";
    #endregion

    #region Excel Export
    public const string EXCEL_MIME_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    #endregion

    #region Colors
    public const string LIGHT_BLUE = "#4f81bd";
    #endregion

    #region Quiz Category Messages
    public const string INVALID_PAGE_NO = "Page number {0} exceeds maximum page number {1}.";
    public const string INVALID_COLUMN_NAME = "Invalid sort column '{0}'. The property does not exist.";
    public const string QUIZ_CATEGORY_NOT_FOUND = "Quiz category with ID {0} was not found.";
    public const string DUPLICATE_QUIZ_CATEGORY = "A quiz category with this name already exists.";
    public const string QUIZ_CATEGORY_DELETED = "Quiz category with ID {0} is already deleted.";
    public const string QUIZ_CATEGORY_STATUS_ALREADY_SET = "Quiz category status is already {0}.";
    public const string QUIZ_CATEGORY_ACTIVATED_SUCCESS = "Quiz category activated successfully.";
    public const string QUIZ_CATEGORY_INACTIVATED_SUCCESS = "Quiz category inactivated successfully.";
    public const string QUIZ_CATEGORY_DELETED_SUCCESS = "Quiz category deleted successfully.";
    public const string QUIZ_CATEGORY_NOT_FOUND_MESSAGE = "The Selected Quiz Category is invalid.";
    #endregion

    #region Quiz Management
    public const string INVALID_QUIZ_STATUS_MESSAGE = "Invalid status. Valid values: 1 (Active), 2 (Draft), 3 (Inactive)";
    public const string CREATE_OR_UPDATE_QUIZ_FAILED = "Failed to create or update quiz.";
    public const string DELETE_QUIZ_FAILED = "Failed to delete quiz.";
    public const string INVALID_EXPORT_REQUEST_QUIZNAME = "Invalid export request. Quiz name is required.";
    public const string INVALID_EXPORT_REQUEST_QUESTIONS = "Invalid export request. No questions provided";
    public const string EXPORT_QUESTIONS_CSV_HEADER = "Question,Type,Difficulty,Category,Option1,Option2,Option3,Option4,CorrectAnswer";
    public const string MISSING_QUESTION_TEXT = "Question text is missing";
    public const string INVALID_QUESTION_TYPE_ID = "Invalid Question Type ID: {0}";
    public const string INVALID_QUESTION_DIFFICULTY_ID = "Invalid Difficulty ID: {0}";
    public const string INVALID_CATEGORY_ID = "Invalid Category ID: {0}";
    public const string INVALID_MCQ_OPTIONS = "Multiple choice question must have at least 2 options";
    public const string NO_CORRECT_ANSWER = "Correct answer is missing for {0} question";
    #endregion

    #region Quiz Difficulty Level Message
    public const string DIFFICULTY_LEVEL_NOT_FOUND = "Difficulty level with ID {0} not found.";
    public const string DUPLICATE_DIFFICULTY_LEVEL_NAME = "Difficulty level with this name already exists.";
    #endregion

    #region Entity Field Names
    public const string IS_DELETED = "IsDeleted";
    public const string ID = "Id";
    #endregion

    #region Linq Function Names
    public const string WHERE = "Where";
    public const string ORDER_BY = "OrderBy";
    #endregion

    #region Question Pool
    public const string EXCEL = "Excel";
    public const string CSV = "CSV";
    public const string QUESTION_TYPE_MULTIPLE_CHOICE = "Multiple Choice";
    public const string QUESTION_TYPE_TRUE_FALSE = "True/False";
    public const string QUESTION_TYPE_SHORT_ANSWER = "Short Answer";
    public const string QUESTION_TYPE_FILL_IN_THE_BLANKS = "Fill in the Blanks";
    public const string QUESTION_KEY_OPTION = "option";
    public const string QUESTION_KEY_ANSWER = "answer";
    public const string INVALID_QUESTION_ID_MESSAGE = "ID must be greater than zero.";
    public const string QUESTION_PREVIEW_FETCH_SUCCESS_MESSAGE = "Question preview fetched successfully.";
    public const string CATEGORY_NOT_FOUND = "Category with id {0} not found.";
    public const string QUESTION_TYPE_NOT_FOUND = "Question type with id {0} not found.";
    public const string DIFFICULTY_NOT_FOUND = "Difficulty with id {0} not found.";
    public const string QUESTION_CREATION_SUCCESS_MESSAGE = "Question created successfully.";
    public const string QUESTION_UPDATE_SUCCESS_MESSAGE = "Question updated successfully.";
    public const string QUESTION_DELETE_SUCCESS_MESSAGE = "Question deleted successfully.";
    public const string QUESTION_NOT_FOUND_ERROR = "Question with id {0} not found or already deleted.";
    public const string CSV_INVALID_OR_EMPTY_ERROR = "CSV is empty or has an invalid format.";
    public const string EXCEL_INVALID_OR_EMPTY_ERROR = "Excel file is empty or has an invalid format.";
    public const string OPTIONS_CANNOT_BE_EMPTY = "Options cannot be empty or null.";
    public const string DUPLICATE_OPTIONS_FOUND = "Duplicate option(s) found: {0}";
    public const string CORRECT_ANSWER_NOT_IN_OPTIONS = "Correct answer '{0}' is not listed among the options.";
    public const string CSV_PREVIEW_LOADED_SUCCESSFULLY = "CSV preview loaded successfully.";
    public const string EXCEL_PREVIEW_LOADED_SUCCESSFULLY = "Excel preview loaded successfully.";
    public const string PROVIDE_PROPER_TEXT_PROMPT = "Provide a valid text prompt.";
    public const string NO_QUESTIONS_TO_SAVE = "No questions to save.";
    public const string QUESTIONS_SAVED_SUCCESSFULLY = "Questions saved successfully.";
    public const string QUESTION_IN_USE_ERROR = "This question is part of an active quiz or battle and cannot be changed.";
    #endregion

    #region User Dashboard
    public const int BATCH_SIZE = 5;
    public const int MIN_BATCH = 1;
    public const int MAX_BATCH = 5;
    public const int MAX_QUIZZES = 15;
    public const string INVALID_BATCH_NUMBER = "Batch number must be between {0} and {1}.";
    public const string USER_DASHBOARD_SUMMARY_FETCH = "User dashboard summary fetched successfully.";
    public const string RECENT_QUIZZES_FETCHED = "Recent Quizzes fetched successfully.";
    public const string FEATURED_QUIZZES_FETCHED = "Featured Quizzes fetched successfully.";
    public const string BATTLE_REQUESTS_FETCHED = "Battle requests fetched successfully.";
    public const string BATTLE_REQUEST_NOT_FOUND = "Battle request with ID {0} not found.";
    public const string BATTLE_STATUS_UPDATED = "Battle request status updated successfully.";
    public const string RANK_PROGRESS_FETCHED = "User rank progress fetched successfully.";
    #endregion

    #region Time Ago Messages
    public const string JUST_NOW = "Just now";
    public const string SECONDS_AGO = "{0} seconds ago";
    public const string MINUTES_AGO = "{0} minutes ago";
    public const string HOURS_AGO = "{0} hours ago";
    public const string DAYS_AGO = "{0} days ago";
    public const string MONTHS_AGO = "{0} months ago";
    public const string YEARS_AGO = "{0} years ago";
    #endregion

    #region Leaderboard
    public const string GLOBAL_LEADERBOARD_RETRIEVED_SUCCESSFULLY = "Global leaderboard data retrieved successfully.";
    public const string USER_LEADERBOARD_STATS_RETRIEVED = "User leaderboard statistics retrieved successfully.";
    public const string CATEGORY_WISE_LEADERBOARD_RETRIEVED = "Category-wise leaderboard retrieved successfully.";
    public const string MONTHLY_CHAMPIONS_RETRIEVED = "Monthly champions retrieved successfully.";
    public const string WEEKLY_LEADERBOARD_RETRIEVED = "Weekly leaderboard retrieved successfully.";
    public const string INVALID_MONTH_MESSAGE = "Invalid month. Please provide a value between 1 and 12.";
    public const string INVALID_YEAR_MESSAGE = "Invalid year. Please provide a valid year between 2023 to present.";
    public const string INVALID_MONTH_YEAR_COMBINATION_MESSAGE = "Invalid month and year combination. The specified month and year cannot be in the future.";
    public const string AVAILABLE_YEARS_RETRIEVED = "Available years retrieved successfully.";
    public const string AVAILABLE_MONTHS_RETRIEVED = "Available months for {0} retrieved successfully.";
    #endregion

    #region Battle Management
    public const string CREATE_OR_UPDATE_BATTLE_FAILED = "Failed to create or update battle.";
    public const string BATTLE_NOT_FOUND = "Battle not found.";
    public const string BATTLE_ALREADY_DELETED = "Battle with ID {0} is already deleted.";
    #endregion

    #region UserProfile
    public const string EMAIL_SUSPENDED = "This account has been suspended. Please contact support.";
    public const string EMAIL_ALREADY_IN_USE = "This email is already in use.";
    public const string EMAIL_INACTIVE = "This account is inactive. Please contact support.";
    public const string FULLNAME_REQUIRED = "Full name is required.";
    public const string OTP_NOT_GENERATED_OR_EXPIRED = "OTP not generated or expired.";
    public const string OTP_EXPIRED = "OTP expired. Please request a new one.";
    public const string OTP_INVALID = "Invalid OTP.";
    #endregion

    #region Plateform Configuration
    public const string PLATFORM_CONFIGURATION_UPDATE_SUCCESS = "Plateform Configuration updated successfully.";
    public const string PLATFORM_CONFIGURATION_NULL_ERROR = "PlatformQuote Configuration is missing or null.";
    public const string PLATFORM_QUOTE_KEY = "PlatformQuote";
    public const string PATH_KEY = "Path";
    public const string PRIMARY_COLOR_KEY = "PrimaryColor";
    public const string SECONDARY_COLOR_KEY = "SecondaryColor";
    public const string IMAGE_SAVE_ERROR = "Failed to save image.";
    #endregion 

    #region Email Templates
    public static readonly Dictionary<EmailTemplateType, string[]> EmailTemplatePlaceholdersRequired = new()
    {
        {
            EmailTemplateType.AccountSuspension,
            new[] { "{{user}}", "{{email}}" }
        },
        {
            EmailTemplateType.BattleRequest,
            new[] { "{{user}}", "{{opponent}}", "{{battleLink}}" }
        },
        {
            EmailTemplateType.EmailVerification,
            new[] { "{{user}}", "{{email}}", "{{otp}}" }
        },
        {
            EmailTemplateType.QuizInvitation,
            new[] { "{{user}}", "{{quizName}}", "{{quizLink}}" }
        },
        {
            EmailTemplateType.ResetPassword,
            new[] { "{{user}}", "{{email}}", "{{resetLink}}" }
        },
        {
            EmailTemplateType.WelComeEmail,
            new[] { "{{user}}", "{{email}}", "{{registrationDate}}", "{{loginUrl}}", "{{year}}", "{{companyName}}" }
        },
        {
            EmailTemplateType.NewUser,
            new[] { "{{user}}", "{{password}}", "{{loginUrl}}" }
        }
    };
    public const string INVALID_EMAIL_TEMPLATE = "Invalid email template.";
    public const string EMAIL_TEMPLATE_ADDED = "Email template added successfully.";
    public const string EMAIL_TEMPLATE_UPDATED = "Email template updated successfully.";
    public const string EMAIL_TEMPLATE_NOT_FOUND = "Email template not found.";
    public const string MISSING_PLACEHOLDER = "Template is missing required placeholder: {0}";
    public const string EMAIL_TEMPLATE_ALREDY_AVAILABLE_FOR_SAME_TYPE = "Template is already available for same type.";
    public const string EMAIL_TEMPLATE_DELETE = "Email template deleted successfully.";
    public const string EMAIL_TEMPLATE_STATUS_UPDATED = "Email template's status updated successfully.";
    public const string INVALID_ACTION = "Invalid action.";
    #endregion

    #region Browse Quizzes
    public const string MIN_PRICE_LESS_THAN_MAX_PRICE = "Min Price cannot be greater than Max Price.";
    public const string MIN_RATING_LESS_THAN_MAX_RATING = "Min Rating cannot be greater than Max Rating.";
    public const string MIN_TOTAL_TIME_LESS_THAN_MAX_TOTAL_TIME = "Min Total Time cannot be greater than Max Total Time.";
    public const int MAX_QUIZ_TOTAL_TIME_MINUTES = 180;
    public const int MIN_QUIZ_TOTAL_TIME_MINUTES = 2;
    public const int MAX_RATING = 5;
    public const string QUIZ_NOT_FOUND = "Quiz not found.";
    public const string QUIZ_NOT_FOUND_OR_COMPLETED = "Quiz not found or already completed.";
    public const string QUIZ_ALREADY_COMPLETED = "Quiz already completed.";
    public const string QUESTION_NOT_FOUND = "Question not found";

    #endregion

    #region 
    public const string OPTIONS_NOT_CONFIGURED = "AI service options are not configured.";
    public const string BASE_URL_CANNOT_BE_EMPTY = "BaseUrl cannot be empty.";
    public const string MODEL_CANNOT_BE_EMPTY = "Model cannot be empty.";
    public const string PROMPT_CANNOT_BE_EMPTY = "Prompt cannot be empty.";

    #endregion

    #region Question Difficulty
    public const string QUESTION_DIFFICULTY_NOT_FOUND = "Question Difficulty not found.";
    public const string QUESTION_DIFFICULTY_ALREADY_EXISTS = "Question Difficulty with name '{0}' already exists.";
    public const string QUESTION_DIFFICULTY_UPDATED = "Question Difficulty updated successfully.";
    public const string QUESTION_DIFFICULTY_ADDED = "Question Difficulty added successfully.";
    public const string QUESTION_DIFFICULTY_DELETED = "Question Difficulty deleted successfully.";
    public const string QUESTION_DIFFICULTY_DUPLICATE_NAME = "Question Difficulty level with this name already exists.";
    public const string QUESTION_DIFFICULTY_DUPLICATE_XP = "Question Difficulty level with {0} xp already exists.";
    #endregion

    #region Quiz
    public const string QUIZ_SUBMITTED_SUCCESSFULLY = "Quiz submitted successfully.";
    public const string QUIZ_ATTEMPT_NOT_FOUND = "The requested quiz attempt was not found.";
    public const string QUIZ_COMPLETED_SUMMARY_FETCHED = "Quiz completed summary fetched successfully.";
    public const string QUIZ_QUESTION_REVIEW_FETCHED = "Quiz question review fetched successfully.";
    public const string DUPLICATE_QUESTION_ISSUE_REPORT = "You have already reported an issue for this question.";
    public const string QUESTION_ISSUE_REPORTED = "Question issue reported successfully.";
    public const string DUPLICATE_QUIZ_RATING = "You have already rated this quiz.";
    public const string QUIZ_RATING_FETCHED = "Quiz rating fetched successfully.";
    public const string QUIZ_RATING_NOT_FOUND = "No quiz rating found for this quiz.";
    public const string QUIZ_RATING_SUBMITTED = "Quiz rating submitted successfully.";
    public const string QUIZ_ANSWER_EXPLANATION_GENERATED = "Answer explanation generated successfully.";
    public const string NO_ANSWER_PROVIDED = "No answer was provided.";
    #endregion

    #region User Battles
    public const string SELF_CHALLENGE_NOT_ALLOWED = "Cannot challenge yourself.";
    public const string ACTIVE_CHALLENGE_EXISTS = "You already have an active challenge sent to this user.";
    public const string BATTLE_REQUEST_SENT_SUCCESS = "Battle request sent successfully.";
    public const string BATTLE_ALREADY_ACCEPTED = "You already have an accepted battle for this Battle and cannot send it to another user.";
    public const string USER_SEARCH_SUCCESS = "User search completed successfully.";
    public const string USER_IN_ACTIVE_BATTLE = "Your friend is already engaged in another battle.";
    public const string USER_IN_ACTIVE_QUIZ = "Your friend is already engaged in a quiz.";
    #endregion

    #region Battle Playing
    public static class SignalRMethods
    {
        public const string CONTINUE_BATTLE = "ContinueBattle";
        public const string ERROR = "Error";
        public const string SEARCHING = "Searching";
        public const string MATCH_FOUND = "MatchFound";
        public const string BATTLE_STARTED = "BattleStarted";
        public const string BATTLE_RESUMED = "BattleResumed";
        public const string RECEIVE_QUESTION = "ReceiveQuestion";
        public const string RECEIVE_SCORE_UPDATE = "ReceiveScoreUpdate";
        public const string LAST_ANSWERED_DETAIL = "LastAnsweredDetail";
        public const string BATTLE_ENDED = "BattleEnded";
        public const string QUESTION_TIMEOUT = "QuestionTimeOut";
        public const string PLAYER_INTERRUPTED = "PlayerInterrupted";
        public const string BATTLE_ENDED_FOR_PARTICULAR_PLAYER_DUE_TO_INTERRUPT = "BattleEndedForParticularPlayerDueToInterrupt";
        public const string RECEIVE_BATTLE_REQUEST = "ReceiveBattleRequest";
        public const string BATTLE_REQUEST_ACCEPTED = "BattleRequestAccepted";
        public const string BATTLE_REQUEST_ACCEPTED_CONFIRMATION = "BattleRequestAcceptedConfirmation";
        public const string BATTLE_REQUEST_ACCEPT_CONFIRMATION = "BattleRequestAcceptConfirmation";
        public const string BATTLE_REQUEST_CANCELLED = "BattleRequestCancelled";
        public const string BATTLE_REQUEST_DECLINED = "BattleRequestDeclined";
    }
    public const string YOU_HAVE_UNFINISHED_BATTLE = "You have an unfinished battle.";
    public const string BATTLE_SESSION_EXPIRED = "Battle session expired.";
    public const string BATTLE_ALREADY_COMPLETED = "Battle already completed.";
    public const string FAILED_TO_CREATE_BATTLE = "Failed to create battle.";
    public const string YOU_ARE_NOT_PART_OF_BATTLE = "You are not part of this battle.";
    public const string RESUME_WINDOW_EXPIRED = "Resume window expired.";
    public const string NO_PENDING_BATTLE_TO_RESUME = "No pending battle to resume.";
    public const string NO_PENDING_BATTLE_TO_INTERRUPT = "No pending battle to interrupt.";
    public const string YOU_HAVE_ALREADY_COMPLETED_THIS_BATTLE = "You have already completed this battle.";
    public const string YOU_ARE_ALREADY_CONNECTED_TO_THIS_BATTLE = "You are already connected to battle.";
    public const string BATTLE_ID = "BattleId";
    public const string BATTLE_ATTEMPT_ID_KEY = "BattleAttemptIdKey";
    public const string INVALID_QUESTION_INDEX = "Invalid question index.";
    public const string ALREADY_ANSWERED = "Already answered this question.";
    public const string ANSWER_CURRENT_QUESTION_ONLY = "Answer the current question only!";
    public const string FAILED_TO_SEND_BATTLE_REQUEST = "Failed to send battle request to user {ReceiverUserId}";
    public const string BOTH_USERS_MUST_BE_ONLINE = "Both users must be online to start a battle.";
    public const string FAILED_TO_START_FRIEND_BATTLE = "Failed to start friend battle.";
    public const string BATTLE_REQUEST_ACCEPT_SENDER_OFFLINE = "Sender did not come online to receive the accept notification.";
    public const string SENDER_IN_ACTIVE_BATTLE_OR_QUIZ = "Your friend is currently busy.";
    public const string BATTLE_REQUEST_NOT_FOUND_OR_ALREADY_HANDLED = "Battle request not found or already handled.";
    #endregion

    #region Generate Questions Using Ai API
    public const string MAX_RETRIES_REACH = "Max retries ({0}) reached for quiz generation";
    public const string QUESTION_GENERATION_FAILED = "We couldn’t generate any questions from your request. Please try again.";
    public const string GENERATION_FAILED = "Question generation failed:";
    public const string FAILED_TO_CLEAN_JSON = "Failed to clean JSON response:";
    public const string FAILED_TO_PASRE_JSON_RESPONSE = "Failed to parse JSON response:";
    public const string MAX_RETRIES_REACHED_DURING_VALIDATION = "Maximum retry attempts reached during validation.";
    public const string CONTENT_VALIDATION_FAILED = "Content validation failed: {0}";
    public const string INVALID_QUESTION_SPECIFICATION = "Invalid or missing question specifications.";
    public const string FAILED_TO_GENERATE_QUESTIONS = "Failed to generate questions.";
    public const string QUESTION_GENERATION_SUCCESS = "Questions generated successfully.";
    public const string UNEXPECTED_ERROR_GENERATING_QUESTIONS = "Unexpected error generating questions.";
    public const string AI_RESPONSE_FORMAT_ERROR = "AI response format error.";
    #endregion

    #region Groq Models
    public const string LLAMA_3_3_70B_VERSATILE = "llama-3.3-70b-versatile";
    public const string LLAMA_3_1_8B_INSTANT = "llama-3.1-8b-instant";
    public const string GROQ_COMPOUND = "groq/compound";
    public const string MOONSHOTAI_KIMI_K2_INSTRUCT = "moonshotai/kimi-k2-instruct";
    public const string OPENAI_GPT_OSS_20B = "openai/gpt-oss-20b";

    public static readonly List<ModelConfig> GroqModels = [
        new ModelConfig(AiModelName.Llama3Point370BVersatile.ToModelString(), 30, 1000, 12000, 100000),
        new ModelConfig(AiModelName.OpenAiGptOss20B.ToModelString(), 30, 1000, 8000, 200000),
        new ModelConfig(AiModelName.Llama3Point18BInstant.ToModelString(), 30, 14400, 6000, 500000),
        new ModelConfig(AiModelName.GroqCompound.ToModelString(), 30, 250, 70000, int.MaxValue),
        new ModelConfig(AiModelName.MoonshotAiKimiK2Instruct.ToModelString(), 60, 1000, 10000, 300000),
    ];
    public static AiModelName GetGroqModelEnumNumber(string modelName)
    {
        return modelName switch
        {
            LLAMA_3_1_8B_INSTANT => AiModelName.Llama3Point18BInstant,
            LLAMA_3_3_70B_VERSATILE => AiModelName.Llama3Point370BVersatile,
            GROQ_COMPOUND => AiModelName.GroqCompound,
            MOONSHOTAI_KIMI_K2_INSTRUCT => AiModelName.MoonshotAiKimiK2Instruct,
            OPENAI_GPT_OSS_20B => AiModelName.OpenAiGptOss20B,
            _ => throw new ArgumentException($"Unknown model name: {modelName}")
        };
    }
    #endregion

    #region Quiz Comment Section
    public const string QUIZ_COMMENTS_FETCHED_SUCCESSFULLY = "Quiz comments fetched successfully.";
    #endregion 
}
