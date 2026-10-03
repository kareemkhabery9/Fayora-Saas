using Fayora.Application.Common.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Fayora.Infrastructure.Persistence.Caching;

public class CacheService(IDistributedCache cache, IServiceProvider serviceProvider) : ICacheService
{
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        string? cachedData = await cache.GetStringAsync(key, cancellationToken);
        if (cachedData is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(key, cancellationToken);
        _knownKeys.TryRemove(key, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var redisConnection = serviceProvider.GetService<IConnectionMultiplexer>();
        if (redisConnection is not null)
        {
            var db = redisConnection.GetDatabase();
            var server = redisConnection.GetServer(redisConnection.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Length > 0)
                await db.KeyDeleteAsync(keys);
        }

        foreach (var key in _knownKeys.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            await cache.RemoveAsync(key, cancellationToken);
            _knownKeys.TryRemove(key, out _);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions();

        options.SetAbsoluteExpiration(absoluteExpirationRelativeToNow ?? TimeSpan.FromHours(1));

        string serializedData = JsonSerializer.Serialize(value);

        await cache.SetStringAsync(key, serializedData, options, cancellationToken);
        _knownKeys[key] = 0;
    }
}
