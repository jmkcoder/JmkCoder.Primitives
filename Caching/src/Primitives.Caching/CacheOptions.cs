namespace Primitives.Caching;

/// <summary>Global options applied to every cache entry unless overridden per-call.</summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Default absolute expiration for entries that do not specify their own.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When <c>true</c> (default) a cache miss exception in <c>factory</c>
    /// propagates to the caller rather than returning <c>default</c>.
    /// </summary>
    public bool PropagateFactoryExceptions { get; set; } = true;

    /// <summary>
    /// Key prefix prepended to every cache key, useful to namespace entries
    /// across multiple instances or environments.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
