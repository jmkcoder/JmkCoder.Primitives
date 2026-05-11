---
layout: default
library: resilience
title: Configuration Reference
description: Full options reference for Primitives.Resilience — all types, properties, and defaults.
permalink: /resilience/reference/
---

## DI extension methods

### `AddPrimitivesResilience`

```csharp
IServiceCollection AddPrimitivesResilience(
    this IServiceCollection services,
    Action<ResilienceOptions>? configure = null)
```

Registers `IResiliencePipelineProvider` as a singleton and optionally configures the full set of
named pipelines. Safe to call multiple times (uses `TryAddSingleton`).

### `AddPrimitivesResiliencePipeline`

```csharp
IServiceCollection AddPrimitivesResiliencePipeline(
    this IServiceCollection services,
    string name,
    Action<PipelineOptions> configure)
```

Adds or replaces a single named pipeline. Can be chained and mixed with `AddPrimitivesResilience`.

---

## `IResiliencePipelineProvider`

```csharp
public interface IResiliencePipelineProvider
{
    ResiliencePipeline    Get(string pipelineName);
    ResiliencePipeline<T> Get<T>(string pipelineName);
}
```

Returns `ResiliencePipeline[<T>].Empty` for unregistered pipeline names — never throws.
Pipelines are built and cached on first access (lazy, thread-safe).

---

## `ResilienceOptions`

```csharp
public sealed class ResilienceOptions
{
    public Dictionary<string, PipelineOptions> Pipelines { get; set; } = new();
}
```

Top-level options registered via `IOptions<ResilienceOptions>`. Binds from configuration:

```json
{
  "Resilience": {
    "Pipelines": {
      "http": {
        "Retry":   { "MaxAttempts": 3 },
        "Timeout": { "Timeout": "00:00:05" }
      }
    }
  }
}
```

```csharp
builder.Services.Configure<ResilienceOptions>(
    builder.Configuration.GetSection("Resilience"));
builder.Services.AddPrimitivesResilience();
```

---

## `PipelineOptions`

```csharp
public sealed class PipelineOptions
{
    public RetryOptions?          Retry          { get; set; }
    public CircuitBreakerOptions? CircuitBreaker { get; set; }
    public TimeoutOptions?        Timeout        { get; set; }
    public BulkheadOptions?       Bulkhead       { get; set; }
}
```

All strategy options are nullable. Set a property to `null` (the default) to omit that strategy.

---

## `RetryOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxAttempts` | `int` | `3` | Number of retries. Total calls = `MaxAttempts + 1`. |
| `BaseDelay` | `TimeSpan` | `00:00:01` | Base delay between retry attempts. |
| `BackoffType` | `BackoffType` | `Exponential` | Delay growth pattern. |
| `UseJitter` | `bool` | `true` | Randomise delay to spread out retry load. |

### `BackoffType` enum

| Value | Description |
|-------|-------------|
| `Constant` | Fixed delay equal to `BaseDelay` between every attempt. |
| `Linear` | Delay grows linearly: `BaseDelay × attempt`. |
| `Exponential` | Delay doubles each attempt: `BaseDelay × 2^attempt`. |

---

## `CircuitBreakerOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FailureRatio` | `double` | `0.5` | Fraction of calls that must fail to open the circuit (0.0–1.0). |
| `MinimumThroughput` | `int` | `10` | Minimum calls in the window before the ratio is evaluated. |
| `SamplingDuration` | `TimeSpan` | `00:00:30` | Sliding time window for failure measurement. |
| `BreakDuration` | `TimeSpan` | `00:00:30` | How long the circuit stays open before probing again. |

Polly type: `Polly.CircuitBreaker.CircuitBreakerStrategyOptions`.  
Rejection exception: `Polly.CircuitBreaker.BrokenCircuitException`.

---

## `TimeoutOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | `00:00:30` | Maximum duration for a single execution attempt. |

Rejection exception: `Polly.Timeout.TimeoutRejectedException`.

---

## `BulkheadOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxConcurrency` | `int` | `10` | Maximum number of concurrent executions allowed. |
| `MaxQueuedItems` | `int` | `0` | Maximum operations that may queue while at capacity. `0` rejects immediately. |

Implemented via `System.Threading.RateLimiting.ConcurrencyLimiter` (Polly v8).  
Rejection exception: `System.Threading.RateLimiting.RateLimiterRejectedException`.

---

## Strategy execution order

```
Retry  →  Circuit Breaker  →  Timeout  →  Bulkhead  →  your delegate
```

Strategies are added to the `ResiliencePipelineBuilder` in this order. Polly executes them
outermost first, meaning Retry wraps CircuitBreaker which wraps Timeout which wraps Bulkhead.

---

## Exceptions summary

| Strategy | Exception thrown on rejection |
|----------|-------------------------------|
| Circuit Breaker (open) | `Polly.CircuitBreaker.BrokenCircuitException` |
| Timeout exceeded | `Polly.Timeout.TimeoutRejectedException` |
| Bulkhead at capacity | `System.Threading.RateLimiting.RateLimiterRejectedException` |
| All retries exhausted | The last exception from the delegate itself |
