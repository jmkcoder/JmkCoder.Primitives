namespace Primitives.RateLimiting.Abstractions;

/// <summary>
/// Persistent counter store used by rate-limit algorithms.
/// Replace the default in-memory store with a Redis or database-backed implementation
/// for distributed deployments.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Increments the counter for <paramref name="key"/> within the current window and
    /// returns the new count. Sets the window expiry on first access.
    /// </summary>
    Task<long> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>Returns the current counter value without incrementing.</summary>
    Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns the remaining TTL of the counter window, or <see cref="TimeSpan.Zero"/> if expired.</summary>
    Task<TimeSpan> GetTtlAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Resets the counter for <paramref name="key"/>.</summary>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
}
