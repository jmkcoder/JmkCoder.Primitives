# Primitives.Resilience

Circuit breakers, retry policies, bulkheads, and timeouts built on Polly v8 — pre-wired for .NET DI with zero boilerplate.

## Package

| Package | Description |
|---------|-------------|
| `Primitives.Resilience` | Named pipeline configuration, `IResiliencePipelineProvider`, and DI registration over Polly v8 |

## Quick start

```bash
dotnet add package Primitives.Resilience
```

```csharp
// Program.cs
builder.Services.AddPrimitivesResilience(o =>
{
    o.Pipelines["payments"] = new PipelineOptions
    {
        Retry          = new RetryOptions          { MaxAttempts = 3 },
        CircuitBreaker = new CircuitBreakerOptions { FailureRatio = 0.5, MinimumThroughput = 10 },
        Timeout        = new TimeoutOptions        { Timeout = TimeSpan.FromSeconds(10) },
    };
});
```

```csharp
// Inject and use
public class PaymentService(IResiliencePipelineProvider pipelines)
{
    public async Task<PaymentResult> ProcessAsync(Payment p, CancellationToken ct) =>
        await pipelines.Get<PaymentResult>("payments")
                       .ExecuteAsync(_ => _client.ChargeAsync(p, ct), ct);
}
```

## Per-pipeline registration

```csharp
builder.Services
    .AddPrimitivesResiliencePipeline("http-client", o =>
    {
        o.Retry   = new RetryOptions   { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(200) };
        o.Timeout = new TimeoutOptions { Timeout = TimeSpan.FromSeconds(5) };
    })
    .AddPrimitivesResiliencePipeline("database", o =>
    {
        o.Retry          = new RetryOptions          { MaxAttempts = 2, BaseDelay = TimeSpan.FromSeconds(1) };
        o.CircuitBreaker = new CircuitBreakerOptions { MinimumThroughput = 5, BreakDuration = TimeSpan.FromSeconds(60) };
    });
```

## Strategies

| Strategy | Options type | Exception when rejected |
|----------|-------------|------------------------|
| Retry | `RetryOptions` | Last exception after all attempts |
| Circuit Breaker | `CircuitBreakerOptions` | `BrokenCircuitException` |
| Timeout | `TimeoutOptions` | `TimeoutRejectedException` |
| Bulkhead | `BulkheadOptions` | `BulkheadRejectedException` |

## Strategy order

Strategies execute **outer → inner**: Retry → CircuitBreaker → Timeout → Bulkhead.

This means:
- **Retry** wraps everything — each retry attempt goes through the circuit breaker, timeout, and bulkhead.
- **CircuitBreaker** tracks cumulative failures across retry groups.
- **Timeout** applies per-attempt — each individual try gets the configured duration.
- **Bulkhead** guards the innermost operation — limits concurrent executions.

## License

MIT
