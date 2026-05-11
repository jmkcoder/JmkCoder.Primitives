namespace Primitives.Resilience.Models;

/// <summary>Determines how the delay between retry attempts grows over time.</summary>
public enum BackoffType
{
    /// <summary>Every retry waits the same duration (<c>BaseDelay</c>).</summary>
    Constant,

    /// <summary>Each retry waits <c>attempt × BaseDelay</c>.</summary>
    Linear,

    /// <summary>Each retry waits <c>2^attempt × BaseDelay</c>.</summary>
    Exponential,
}
