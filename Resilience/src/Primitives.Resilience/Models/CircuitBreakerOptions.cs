namespace Primitives.Resilience.Models;

/// <summary>Configuration for the circuit-breaker strategy.</summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Ratio of failed calls (0.0–1.0) within the sampling window that causes
    /// the circuit to open. Default: <c>0.5</c> (50 %).
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Minimum number of calls required in the sampling window before the
    /// failure ratio is evaluated. Prevents the circuit from opening on a
    /// single failure at low throughput. Default: <c>10</c>.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Duration of the sliding window over which failures are measured.
    /// Default: <c>30 seconds</c>.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the circuit stays open (and rejects calls with
    /// <c>BrokenCircuitException</c>) before transitioning to half-open.
    /// Default: <c>30 seconds</c>.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}
