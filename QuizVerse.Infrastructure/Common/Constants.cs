namespace QuizVerse.Infrastructure.Common;

public static class Constants
{
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
    public const string NO_DATA_FOUND = "No data found.";
    public const string UNAUTHORIZED_USER = "User ID missing";
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
    #endregion

    #region Email Messages
    public const string EMAIL_PATH_NOT_CONFIGURED = "Email template path is not configured.";
    public const string EMAIL_SENT_SUCCESS = "Email successfully sent to {0}.";
    public const string USER_REGISTERED_AND_EMAIL_SENT = "User registered successfully. A welcome email has been sent.";
    public const string USER_REGISTERED_BUT_EMAIL_NOT_SENT = "User registered successfully, but email could not be sent.";
    public const string EMAIL_NOT_SENT = "Email not sent.";
    public const string SMTP_CONFIG_MISSING = "SMTP configuration is missing required fields.";
    #endregion

    #region EmailTemplateConstants
    public const string NEW_USER_TEMPLATE_PATH = "Templates/NewUser.html";
    public const string REGISTER_USER_TEMPLATE_PATH = "Templates/Welcome.html";
    #endregion

    #region CRUD Messages
    public const string FETCH_SUCCESS = "Data fetched successfully";
    public const string CREATE_SUCCESS = "Created successfully";
    public const string UPDATE_SUCCESS = "Updated successfully";
    public const string DELETE_SUCCESS = "Deleted successfully";
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
    public const string QUIZ_CATEGORY_NOT_FOUND = "Quiz category with ID {0} not found.";
    public const string DUPLICATE_QUIZZ_CATEGORY = "Quiz with this category already exist";
    public const string QUIZ_CATEGORY_DELETED = "Quiz with this category {0} is already deleted.";
    public const string QUIZ_CATEGORY_STATUS_ALREADY_SET = "User is already {0}.";
    public const string QUIZ_CATEGORY_STATUS_CHANGED_SUCCESS = "User {0} status changed to {1}.";
    public const string QUIZ_CATEGORY_DEFAULT_ICON = "category"; 
    #endregion

    #region Quiz Management List
    public const string INVALID_QUIZ_STATUS_MESSAGE = "Invalid status. Valid values: 1 (Active), 2 (Draft), 3 (Inactive)";
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
    public const string NO_VALID_QUESTIONS_FOUND_IN_FILE_ERROR = "No valid question data found in the uploaded {0} file.";
    public const string QUESTIONS_IMPORTED_SUCCESS_MESSAGE = "{0} questions imported successfully from {1}.";
    #endregion
}
