namespace Primitives.Resilience.Models;

/// <summary>Configuration for the per-attempt timeout strategy.</summary>
public sealed class TimeoutOptions
{
    /// <summary>
    /// Maximum duration allowed for a single execution attempt.
    /// Exceeding this causes <c>TimeoutRejectedException</c>.
    /// Default: <c>30 seconds</c>.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
