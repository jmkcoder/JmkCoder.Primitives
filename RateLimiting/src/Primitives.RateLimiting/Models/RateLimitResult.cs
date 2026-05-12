namespace Primitives.RateLimiting.Models;

/// <summary>The result of a rate-limit acquire or peek operation.</summary>
public sealed class RateLimitResult
{
    /// <summary><see langword="true"/> when the request is within the allowed limit.</summary>
    public required bool IsAllowed { get; init; }

    /// <summary>Number of permits consumed so far in the current window.</summary>
    public required long Count { get; init; }

    /// <summary>Maximum permits allowed per window.</summary>
    public required long Limit { get; init; }

    /// <summary>Remaining permits before the limit is reached. Zero when throttled.</summary>
    public long Remaining => Math.Max(0, Limit - Count);

    /// <summary>Approximate time until the current window resets.</summary>
    public required TimeSpan RetryAfter { get; init; }
}
