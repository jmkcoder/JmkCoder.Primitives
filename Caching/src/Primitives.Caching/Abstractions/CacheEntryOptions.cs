namespace Primitives.Caching.Abstractions;

/// <summary>
/// Controls how long a cache entry lives and which tags are associated with it.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Absolute expiry measured from the moment the entry is stored.
    /// When <c>null</c> the provider's configured default is used.
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Sliding expiry — resets each time the entry is read.
    /// Mutually exclusive with <see cref="AbsoluteExpiration"/>; if both
    /// are set, absolute expiration takes precedence.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Tags that can be used to invalidate this entry along with others
    /// sharing the same tag via <see cref="ICacheService.InvalidateByTagAsync"/>.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];
}
