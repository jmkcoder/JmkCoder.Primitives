using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;
using Primitives.Resilience.Models;

namespace Primitives.Resilience.Internal;

/// <summary>
/// Translates <see cref="PipelineOptions"/> into Polly strategy calls.
/// Polly v8 strategy extension methods are defined on the concrete builder types,
/// not on <see cref="ResiliencePipelineBuilderBase"/>, so two overloads are required.
/// </summary>
internal static class PipelineConfigurator
{
    internal static void Configure(ResiliencePipelineBuilder builder, PipelineOptions options)
    {
        ApplyStrategies(builder, options);
    }

    internal static void Configure<T>(ResiliencePipelineBuilder<T> builder, PipelineOptions options)
    {
        ApplyStrategies(builder, options);
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static void ApplyStrategies(ResiliencePipelineBuilder builder, PipelineOptions options)
    {
        if (options.Retry is { } retry)
            builder.AddRetry(BuildRetryOptions(retry));

        if (options.CircuitBreaker is { } cb)
            builder.AddCircuitBreaker(BuildCircuitBreakerOptions(cb));

        if (options.Timeout is { } timeout)
            builder.AddTimeout(timeout.Timeout);

        if (options.Bulkhead is { } bulkhead)
            builder.AddConcurrencyLimiter(bulkhead.MaxConcurrency, bulkhead.MaxQueuedItems);
    }

    private static void ApplyStrategies<T>(ResiliencePipelineBuilder<T> builder, PipelineOptions options)
    {
        if (options.Retry is { } retry)
            builder.AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = retry.MaxAttempts,
                Delay            = retry.BaseDelay,
                BackoffType      = MapBackoffType(retry.BackoffType),
                UseJitter        = retry.UseJitter,
            });

        if (options.CircuitBreaker is { } cb)
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio      = cb.FailureRatio,
                MinimumThroughput = cb.MinimumThroughput,
                SamplingDuration  = cb.SamplingDuration,
                BreakDuration     = cb.BreakDuration,
            });

        if (options.Timeout is { } timeout)
            builder.AddTimeout(timeout.Timeout);

        if (options.Bulkhead is { } bulkhead)
            builder.AddConcurrencyLimiter(bulkhead.MaxConcurrency, bulkhead.MaxQueuedItems);
    }

    private static RetryStrategyOptions BuildRetryOptions(RetryOptions retry) => new()
    {
        MaxRetryAttempts = retry.MaxAttempts,
        Delay            = retry.BaseDelay,
        BackoffType      = MapBackoffType(retry.BackoffType),
        UseJitter        = retry.UseJitter,
    };

    private static CircuitBreakerStrategyOptions BuildCircuitBreakerOptions(CircuitBreakerOptions cb) => new()
    {
        FailureRatio      = cb.FailureRatio,
        MinimumThroughput = cb.MinimumThroughput,
        SamplingDuration  = cb.SamplingDuration,
        BreakDuration     = cb.BreakDuration,
    };

    private static DelayBackoffType MapBackoffType(BackoffType type) => type switch
    {
        BackoffType.Constant    => DelayBackoffType.Constant,
        BackoffType.Linear      => DelayBackoffType.Linear,
        BackoffType.Exponential => DelayBackoffType.Exponential,
        _                       => DelayBackoffType.Exponential,
    };
}
