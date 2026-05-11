---
layout: default
library: resilience
title: Strategies
description: Retry, circuit breaker, timeout, and bulkhead — how each strategy works and when to use it.
permalink: /resilience/strategies/
---

## Strategy execution order

When multiple strategies are combined in a single pipeline, they execute **outer → inner**:

```
Retry  →  Circuit Breaker  →  Timeout  →  Bulkhead  →  your code
```

- **Retry** is outermost — it re-executes everything below on failure.
- **Circuit Breaker** tracks cumulative failures across retry groups.
- **Timeout** gives each individual attempt a deadline.
- **Bulkhead** guards the actual operation against concurrency overload.

---

## Retry

Retries a failed operation up to `MaxAttempts` times before propagating the final exception.

```csharp
new PipelineOptions
{
    Retry = new RetryOptions
    {
        MaxAttempts = 3,
        BaseDelay   = TimeSpan.FromMilliseconds(200),
        BackoffType = BackoffType.Exponential,  // 200ms → 400ms → 800ms
        UseJitter   = true,                     // adds randomness to prevent thundering herd
    },
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `MaxAttempts` | `3` | Number of retries. Total calls = `MaxAttempts + 1`. |
| `BaseDelay` | `1 second` | Base delay between retries. |
| `BackoffType` | `Exponential` | `Constant`, `Linear`, or `Exponential` growth. |
| `UseJitter` | `true` | Add random jitter to reduce retry storms. |

**When to use:** Transient failures — network timeouts, throttling responses, brief database
unavailability.

**Do not retry:** Non-idempotent operations (POST payments, sends) without deduplication, or
exceptions that indicate a logic error (e.g. `ArgumentException`).

---

## Circuit Breaker

Opens the circuit (rejects calls with `BrokenCircuitException`) when too many failures occur in a
sliding time window. After the break duration, the circuit transitions to half-open and allows a
probe request through.

```csharp
new PipelineOptions
{
    CircuitBreaker = new CircuitBreakerOptions
    {
        FailureRatio      = 0.5,                       // open if ≥ 50 % of calls fail
        MinimumThroughput = 10,                        // require at least 10 calls before evaluating
        SamplingDuration  = TimeSpan.FromSeconds(30),  // failure ratio measured over 30 s
        BreakDuration     = TimeSpan.FromSeconds(30),  // stay open for 30 s
    },
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `FailureRatio` | `0.5` | Failure ratio (0.0–1.0) that triggers the circuit opening. |
| `MinimumThroughput` | `10` | Minimum calls in the window before the ratio is evaluated. |
| `SamplingDuration` | `30 seconds` | Sliding window for failure measurement. |
| `BreakDuration` | `30 seconds` | How long the circuit stays open before trying again. |

**When to use:** Wrap calls to external services to shed load fast and give the downstream system
time to recover.

**In combination with retry:** Place CircuitBreaker inside Retry (the default order). When the
circuit is open, retry attempts will immediately throw `BrokenCircuitException` — the retry
strategy's default `ShouldHandle` predicate does not retry on circuit-open exceptions, so the
circuit break propagates immediately.

---

## Timeout

Cancels the operation if it does not complete within the configured duration and throws
`TimeoutRejectedException`.

```csharp
new PipelineOptions
{
    Timeout = new TimeoutOptions
    {
        Timeout = TimeSpan.FromSeconds(5),
    },
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `Timeout` | `30 seconds` | Maximum duration for a single execution attempt. |

**Per-attempt vs. total timeout.** This timeout applies to each individual attempt. Combined with
retry, a pipeline with 3 retries and a 5-second timeout will allow up to `4 × 5 = 20 seconds`
total. For a hard total deadline, wrap the pipeline call with `CancellationTokenSource.CancelAfter`
in your application code.

<div class="bd-callout bd-callout-warning">
<strong>Always observe the CancellationToken.</strong> The timeout strategy signals cancellation via the
<code>CancellationToken</code> passed to your delegate. If your code ignores it (e.g. blocking I/O),
the timeout will not take effect until the next <code>await</code>.
</div>

---

## Bulkhead (Concurrency Limiter)

Limits the number of operations that may execute concurrently. Calls beyond `MaxConcurrency` are
either queued (up to `MaxQueuedItems`) or rejected immediately with `BulkheadRejectedException`.

```csharp
new PipelineOptions
{
    Bulkhead = new BulkheadOptions
    {
        MaxConcurrency  = 10,  // at most 10 simultaneous calls
        MaxQueuedItems  = 5,   // up to 5 more may wait; beyond that, reject immediately
    },
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `MaxConcurrency` | `10` | Maximum concurrent in-flight operations. |
| `MaxQueuedItems` | `0` | Maximum operations waiting in the queue. `0` = reject immediately if at capacity. |

**When to use:** Protect a downstream resource (database pool, rate-limited API) from being
overwhelmed by high concurrency spikes.
