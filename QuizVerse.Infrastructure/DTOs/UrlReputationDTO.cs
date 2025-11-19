using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs;

// Full response of analyse url function
public class UrlReputationResult
{
    public bool IsUnsafe { get; set; }
    public List<string> Categories { get; set; } = new();
}

//For Gemin response
public class GeminiResponse { public GeminiCandidate[]? Candidates { get; set; } }

public class GeminiCandidate { public GeminiContent? Content { get; set; } }

public class GeminiContent { public GeminiPart[]? Parts { get; set; } }

public class GeminiPart { public string? Text { get; set; } }

// For Model rotations
public class GeminiModelConfig
{
    public AiModelName Name { get; set; } 
    public string ApiEndpoint { get; set; } = "";
    public int RPM { get; set; }
    public int RPD { get; set; }
    public int Priority { get; set; }
}

public class ModelUsageStats
{
    public int RequestsToday { get; set; }
    public DateTime LastResetDate { get; set; }
    public List<DateTime> RecentRequests { get; set; } = new List<DateTime>();
}

public class ModelRotationState
{
    public Dictionary<AiModelName, ModelUsageStats> ModelStats { get; set; } = new Dictionary<AiModelName, ModelUsageStats>();
    public AiModelName CurrentActiveModel { get; set; } 
}

public class ServiceCheckResult
{
    public bool Success { get; set; }
    public bool IsUnsafe { get; set; }
    public string Message { get; set; } = string.Empty;
}