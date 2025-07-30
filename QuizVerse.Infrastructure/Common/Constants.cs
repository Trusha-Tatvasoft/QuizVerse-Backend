namespace QuizVerse.Infrastructure.Common;

public static class Constants
{
    #region General Messages
    public const string QUIZVERSE_DEFAULT_QUOTE = "Welcome to QuizVerse!";
    public const string FETCH_DATA_MESSAGE = "Data Fetched Successfully.";
    public const string INVALID_DATA_MESSAGE = "Invalid Data.";
    public const string NULL_MODIFIED_DATE_MESSAGE = "Modified Date is null.";
    public const string INVALID_USER_ID_MESSAGE = "Invalid UserId.";
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
    public const string REGISTER_USER_TEMPLATE_PATH  = "Templates/Welcome.html";
    #endregion
    
    #region CRUD Messages
    public const string FETCH_SUCCESS = "Data fetched successfully";
    public const string CREATE_SUCCESS = "Created successfully";
    public const string UPDATE_SUCCESS = "Updated successfully";
    public const string DELETE_SUCCESS = "Deleted successfully";
    #endregion
}
