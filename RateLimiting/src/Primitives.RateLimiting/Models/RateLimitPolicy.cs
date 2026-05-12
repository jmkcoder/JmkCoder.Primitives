namespace Primitives.RateLimiting.Models;

/// <summary>Defines a named rate-limit policy.</summary>
public sealed class RateLimitPolicy
{
    /// <summary>Unique policy name (e.g. <c>"api-default"</c>, <c>"login"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Maximum number of permitted requests per <see cref="Window"/>.</summary>
    public required long PermitLimit { get; init; }

    /// <summary>Duration of the sliding or fixed window.</summary>
    public required TimeSpan Window { get; init; }

    /// <summary>Algorithm to apply. Defaults to <see cref="RateLimitAlgorithm.SlidingWindow"/>.</summary>
    public RateLimitAlgorithm Algorithm { get; init; } = RateLimitAlgorithm.SlidingWindow;
}
