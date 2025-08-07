using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace QuizVerse.Application.Core.Service;

public class MemoryCacheService(IMemoryCache _memoryCache, IConfiguration _configuration) : IMemoryCacheService
{
    public T GetOrSet<T>(string key, Func<T> getData)
    {
        var result = _memoryCache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(double.Parse(_configuration["MemoryCacheSetting:MemoryCacheExpiryHours"]!));
            return getData();
        }) ?? throw new KeyNotFoundException();
        return result;
    }

    public void Clear(string key)
    {
        _memoryCache.Remove(key);
    }
}