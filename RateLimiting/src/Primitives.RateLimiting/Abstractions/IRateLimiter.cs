using Primitives.RateLimiting.Models;

namespace Primitives.RateLimiting.Abstractions;

/// <summary>
/// Checks and records request counts against configured rate-limit policies.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a permit for the given <paramref name="key"/> under the named
    /// <paramref name="policy"/>. Returns a <see cref="RateLimitResult"/> describing whether
    /// the request is allowed and how many permits remain.
    /// </summary>
    Task<RateLimitResult> AcquireAsync(
        string policy,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current window status for <paramref name="key"/> without consuming a permit.
    /// </summary>
    Task<RateLimitResult> PeekAsync(
        string policy,
        string key,
        CancellationToken cancellationToken = default);
}
