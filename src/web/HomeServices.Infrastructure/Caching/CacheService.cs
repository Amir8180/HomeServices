using System.Text.Json;
using HomeServices.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HomeServices.Infrastructure.Caching;

/// <summary>
/// Dual-mode cache service. When a Redis connection string is configured the
/// IDistributedCache (Redis) is used; otherwise it falls back to the in-memory
/// cache. JSON serialisation keeps payloads compact and portable. The prefix-based
/// invalidation walks the in-memory keys collection (memory only) — for Redis a
/// versioned-key strategy is recommended for bulk invalidation in production.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache? _distributed;
    private readonly IMemoryCache? _memory;
    private readonly ILogger<CacheService> _logger;
    private readonly bool _useRedis;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    public CacheService(
        IServiceProvider provider,
        ILogger<CacheService> logger)
    {
        _logger = logger;
        // Resolve optionally — distributed cache is registered only when Redis is configured.
        _distributed = (IDistributedCache?)provider.GetService(typeof(IDistributedCache));
        _memory = (IMemoryCache?)provider.GetService(typeof(IMemoryCache));
        // The in-memory fallback registers MemoryDistributedCache; treat that as "no Redis".
        _useRedis = _distributed is not null &&
                    _distributed.GetType().Name != nameof(Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_useRedis && _distributed is not null)
        {
            var bytes = await _distributed.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0) return default;
            return Deserialize<T>(bytes);
        }

        if (_memory is not null && _memory.TryGetValue(key, out var value) && value is T typed)
            return typed;

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        var ttl = absoluteExpiration ?? _defaultTtl;

        if (_useRedis && _distributed is not null)
        {
            var bytes = Serialize(value);
            await _distributed.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            }, cancellationToken);
            return;
        }

        if (_memory is not null)
        {
            _memory.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            });
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_useRedis && _distributed is not null)
        {
            await _distributed.RemoveAsync(key, cancellationToken);
            return;
        }

        _memory?.Remove(key);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null) return existing;

        var value = await factory();
        if (value is not null)
            await SetAsync(key, value, absoluteExpiration, cancellationToken);

        return value;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Note: prefix invalidation on Redis requires a tagged/versioned key scheme.
        // The memory cache is exhausted here; distributed caches should be flushed via
        // a version bump. Kept simple for the local/development default.
        if (_useRedis)
        {
            _logger.LogDebug("RemoveByPrefix('{Prefix}') is a no-op for Redis; use versioned keys for bulk invalidation.", prefix);
            return Task.CompletedTask;
        }

        if (_memory is MemoryCache concrete)
        {
            // Compact clears expired entries; best-effort prefix sweep.
            var field = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(concrete) is IDictionary<dynamic, dynamic> entries)
            {
                var keysToRemove = new List<string>();
                foreach (var key in entries.Keys)
                {
                    if (key is string s && s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        keysToRemove.Add(s);
                }
                foreach (var k in keysToRemove)
                    _memory.Remove(k);
            }
        }

        return Task.CompletedTask;
    }

    private static byte[] Serialize<T>(T value)
        => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    private static T? Deserialize<T>(byte[] bytes)
        => JsonSerializer.Deserialize<T>(bytes, JsonOptions);
}
