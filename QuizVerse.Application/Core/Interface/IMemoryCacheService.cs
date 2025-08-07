namespace QuizVerse.Application.Core.Service;

public interface IMemoryCacheService
{
    T GetOrSet<T>(string key, Func<T> getData);
    void Clear(string key);
}