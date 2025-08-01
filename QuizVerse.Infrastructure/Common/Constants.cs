namespace QuizVerse.Infrastructure.Common;

public static class Constants
{
    #region General Messages
    public const string PLATFORM_NAME = "QuizVerse";
    public const string QUIZVERSE_DEFAULT_QUOTE = "Welcome to QuizVerse!";
    public const string FETCH_DATA_MESSAGE = "Data Fetched Successfully.";
    public const string INVALID_DATA_MESSAGE = "Invalid Data.";
    public const string NULL_MODIFIED_DATE_MESSAGE = "Modified Date is null.";
    public const string INVALID_USER_ID_MESSAGE = "Invalid UserId.";
    public const string INVALID_STATUS_MESSAGE = "Invalid status. Valid values: 1 (Active), 2 (Inactive), 3 (Suspend)";
    public const string INVALID_ROLE_MESSAGE = "Invalid role. Valid values: 1 (Admin), 2 (Player)";
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

    #region Excel Export
    public const string EXCEL_MIME_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    #endregion

    #region Colors
    public const string LIGHT_BLUE = "#4f81bd"; 
    #endregion
}
