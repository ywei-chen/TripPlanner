using Microsoft.Extensions.Caching.Memory;
using TripPlanner.Application.Common.Interfaces;

namespace TripPlanner.Infrastructure.Cache;

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public Task<T?> GetAsync<T>(string key)
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        cache.Set(key, value, expiry ?? DefaultExpiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(cache.TryGetValue(key, out _));
    }

    public Task IncrementAsync(string key)
    {
        var count = cache.GetOrCreate(key, e => { e.SlidingExpiration = TimeSpan.FromHours(1); return 0L; });
        cache.Set(key, count + 1, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task<long> GetCounterAsync(string key)
    {
        cache.TryGetValue(key, out long count);
        return Task.FromResult(count);
    }
}
