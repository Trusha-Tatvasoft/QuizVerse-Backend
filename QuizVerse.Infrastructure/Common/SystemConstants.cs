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
}
