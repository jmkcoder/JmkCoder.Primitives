namespace Primitives.Caching.Abstractions;

/// <summary>
/// Core cache service. Provides cache-aside, explicit set/get, and
/// invalidation over the configured backend (in-memory or distributed).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or invokes
    /// <paramref name="factory"/> to produce it, stores the result, and
    /// returns it.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the cached value, or <c>default</c> if absent.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the entry for <paramref name="key"/>.</summary>
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all entries that were stored with the given <paramref name="tag"/>.
    /// </summary>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);
}
