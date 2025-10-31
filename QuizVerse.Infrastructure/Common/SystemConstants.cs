namespace QuizVerse.Infrastructure.Common;

public class SystemConstants
{
    public const string CORS_POLICY_NAME = "QuizverseCorsPolicy";
    public const string SYSTEM_VERSION = "v1";
    public const string SWAGGER_PAGE_TITLE = "QuizVerse API";
    public const string SCEURITY_SCHEME = "Bearer";
    public const string JWT_ACCESS_TOKEN_HEADER_NAME = "Authorization";
    public const string BEARER_FORMAT = "JWT";
    public const string HEADER_TOKEN_DESCRIPTION = @"Bearer token.";
    public const string DB_CONNECTION_STRING_NAME = "DefaultConnection";
    public const string JWT_CONFIGURATION_KEY = "JwtSettings:Key";
    public const string JWT_CONFIGURATION_ISSUER = "JwtSettings:Issuer";
    public const string JWT_CONFIGURATION_AUDIENCE = "JwtSettings:Audience";
    public const string REMEMBER_ME_CLAIM_NAME = "remember_me";
    public const int DEFAULT_PAGE_SIZE = 10;
    public const int IMAGE_UPLOAD_MAX_SIZE = 10 * 1024 * 1024;
    public static readonly string[] IMAGE_ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".gif"];
    public const string PLATFORM_QUOTE_CONFIGURATION_NAME = "Platform Quote";
    public const string PLATFORM_LOGO_CONFIGURATION_NAME = "Logo";
    public const string PLATFORM_COLOR_CONFIGURATION_NAME = "Colors";
    public const string LOGO_PATH = "wwwroot/uploads/logo";
    public const string LOGO_FOLDER_NAME = "logo";
    public const string DEFAULT_PRIMARY_COLOR = "#9333ea";
    public const string DEFAULT_SECONDARY_COLOR = "#2563eb";
    public const string DEFAULT_PLATFORM_QUOTE_JSON =
        "{\"PlatformQuote\":\"Create, share, and compete in quizzes powered by AI. Challenge friends, join tournaments, and climb the leaderboards in the ultimate quiz experience.\"}";
    public const string DEFAULT_PLATFORM_LOGO_JSON =
        "{\"Path\":\"default-logo/default-logo.png\"}";
    public const string AI_SERVICE_SETTINGS = "AiServiceSettings";
    public const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";
}
