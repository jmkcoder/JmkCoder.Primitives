namespace Primitives.Resilience.Models;

/// <summary>Configuration for the retry strategy.</summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts. Total calls = <c>MaxAttempts + 1</c>.
    /// Default: <c>3</c>.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts. The actual delay is computed from
    /// <see cref="BackoffType"/>. Default: <c>1 second</c>.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How the delay grows between attempts.
    /// Default: <see cref="BackoffType.Exponential"/>.
    /// </summary>
    public BackoffType BackoffType { get; set; } = BackoffType.Exponential;

    /// <summary>
    /// Adds a random jitter to the computed delay to avoid thundering-herd problems.
    /// Default: <c>true</c>.
    /// </summary>
    public bool UseJitter { get; set; } = true;
}
