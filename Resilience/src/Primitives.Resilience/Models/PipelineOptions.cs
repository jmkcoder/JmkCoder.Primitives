namespace Primitives.Resilience.Models;

/// <summary>
/// Configures the strategies applied to a named resilience pipeline.
/// All strategy properties are optional — omit any that are not needed.
/// Strategies are applied in the order: Retry → CircuitBreaker → Timeout → Bulkhead
/// (outer-to-inner), so a retry wraps all inner strategies.
/// </summary>
public sealed class PipelineOptions
{
    /// <summary>Retry policy. <c>null</c> disables retry.</summary>
    public RetryOptions? Retry { get; set; }

    /// <summary>Circuit-breaker policy. <c>null</c> disables circuit breaking.</summary>
    public CircuitBreakerOptions? CircuitBreaker { get; set; }

    /// <summary>Per-attempt timeout. <c>null</c> disables timeout.</summary>
    public TimeoutOptions? Timeout { get; set; }

    /// <summary>Concurrency limiter (bulkhead). <c>null</c> disables bulkhead.</summary>
    public BulkheadOptions? Bulkhead { get; set; }
}
