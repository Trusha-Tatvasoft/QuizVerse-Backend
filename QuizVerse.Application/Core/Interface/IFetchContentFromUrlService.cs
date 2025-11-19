namespace QuizVerse.Application.Core.Interface;

public interface IFetchContentFromUrlService
{
    Task<string> FetchAndValidateAsync(string url);
}
