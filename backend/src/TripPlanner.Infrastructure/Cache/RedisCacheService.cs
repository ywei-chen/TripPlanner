using System.Text.Json;
using StackExchange.Redis;
using TripPlanner.Application.Common.Interfaces;

namespace TripPlanner.Infrastructure.Cache;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public Task RemoveAsync(string key) => _db.KeyDeleteAsync(key);

    public Task<bool> ExistsAsync(string key) => _db.KeyExistsAsync(key);

    public Task IncrementAsync(string key) => _db.StringIncrementAsync(key);

    public async Task<long> GetCounterAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.TryParse(out long result) ? result : 0;
    }
}
