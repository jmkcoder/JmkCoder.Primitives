namespace Primitives.Resilience.Models;

/// <summary>
/// Configuration for the bulkhead (concurrency limiter) strategy.
/// Limits the number of concurrent executions to protect downstream resources
/// from being overwhelmed.
/// </summary>
public sealed class BulkheadOptions
{
    /// <summary>
    /// Maximum number of operations that may execute concurrently.
    /// Default: <c>10</c>.
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Maximum number of operations that may queue while waiting for a permit.
    /// When the queue is full, further calls are rejected immediately with
    /// <c>BulkheadRejectedException</c>. Default: <c>0</c> (no queuing).
    /// </summary>
    public int MaxQueuedItems { get; set; } = 0;
}
