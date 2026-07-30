namespace HomeServices.Application.Interfaces;

/// <summary>
/// Abstraction over the cache layer. The default implementation uses the in-memory
/// cache; when Redis is configured it transparently switches to a distributed cache.
/// Centralises the cache key scheme and (de)serialisation so callers stay simple.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
