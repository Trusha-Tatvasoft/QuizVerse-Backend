namespace QuizVerse.Infrastructure.Common;

public class ModelConfig(string modelName, int maxRequestsPerMinute, int maxRequestsPerDay,
                  int tokensPerMinute, int tokensPerDay)
{
    public string ModelName { get; } = modelName;
    public int MaxRequestsPerMinute { get; } = maxRequestsPerMinute;
    public int MaxRequestsPerDay { get; } = maxRequestsPerDay;
    public int TokensPerMinute { get; } = tokensPerMinute;
    public int TokensPerDay { get; } = tokensPerDay;

    // Token tracking
    public int TokensUsedPerMinute { get; private set; } = 0;
    public int TokensUsedPerDay { get; private set; } = 0;

    // Request tracking
    public int RequestsUsedPerMinute { get; private set; } = 0;
    public int RequestsUsedPerDay { get; private set; } = 0;

    private DateTime? _blockedUntil = null;

    public bool IsLimitReached()
    {
        if (_blockedUntil.HasValue && DateTime.UtcNow < _blockedUntil.Value)
            return true;

        // Check token limits
        bool tokenLimitReached = TokensUsedPerMinute >= TokensPerMinute ||
                                TokensUsedPerDay >= TokensPerDay;

        // Check request limits
        bool requestLimitReached = RequestsUsedPerMinute >= MaxRequestsPerMinute ||
                                   RequestsUsedPerDay >= MaxRequestsPerDay;

        return tokenLimitReached || requestLimitReached;
    }

    public void RecordRequest(int tokensUsed)
    {
        // Ensure we don't record negative tokens
        if (tokensUsed < 0)
        {
            tokensUsed = 0;
        }

        TokensUsedPerMinute += tokensUsed;
        TokensUsedPerDay += tokensUsed;
        RequestsUsedPerMinute++;
        RequestsUsedPerDay++;
    }

    public void ResetPerMinuteUsage()
    {
        TokensUsedPerMinute = 0;
        RequestsUsedPerMinute = 0;
        _blockedUntil = null;
    }

    public void ResetPerDayUsage()
    {
        TokensUsedPerDay = 0;
        RequestsUsedPerDay = 0;
        _blockedUntil = null;
    }

    public void MarkAsTemporarilyBlocked(int seconds)
    {
        _blockedUntil = DateTime.UtcNow.AddSeconds(seconds);
    }
}