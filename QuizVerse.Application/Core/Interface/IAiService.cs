namespace QuizVerse.Application.Core.Interface;

public interface IAiService
{
    public Task<string> GetResponseAsync(string prompt);
}