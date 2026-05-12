namespace Primitives.RateLimiting.Models;

/// <summary>Rate-limiting algorithm variants.</summary>
public enum RateLimitAlgorithm
{
    /// <summary>Fixed window — counts reset at the end of each window boundary.</summary>
    FixedWindow,

    /// <summary>Sliding window — counts are weighted across the current and previous windows.</summary>
    SlidingWindow,
}
