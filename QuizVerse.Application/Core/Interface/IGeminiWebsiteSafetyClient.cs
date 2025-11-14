namespace QuizVerse.Application.Core.Interface;

public interface IGeminiWebsiteSafetyClient
{
    Task<(bool IsUnsafe, string Message)> IsUnsafeAsync(string url);
}