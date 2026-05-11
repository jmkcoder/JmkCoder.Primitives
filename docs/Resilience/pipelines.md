---
layout: default
library: resilience
title: Named Pipelines
description: Design per-service resilience pipelines, mix strategies, and retrieve them with IResiliencePipelineProvider.
permalink: /resilience/pipelines/
---

## Why named pipelines?

Different call sites need different resilience characteristics:

- An HTTP client call to a third-party API needs retry + per-attempt timeout.
- A database read needs retry + circuit breaker.
- A payment charge needs a circuit breaker only (retrying a charge risks double-charging).
- A background data sync needs a bulkhead to limit concurrency.

Named pipelines let you configure these differences in one place and reference them by name
throughout your application, keeping call sites free of policy configuration.

---

## Defining pipelines

### Option A — `AddPrimitivesResilience` (all at once)

```csharp
builder.Services.AddPrimitivesResilience(o =>
{
    // Retrying HTTP calls to a rate-limited third-party API
    o.Pipelines["third-party-api"] = new PipelineOptions
    {
        Retry   = new RetryOptions   { MaxAttempts = 3, BaseDelay = TimeSpan.FromSeconds(1) },
        Timeout = new TimeoutOptions { Timeout = TimeSpan.FromSeconds(10) },
    };

    // Database reads — circuit breaker prevents hammering a degraded DB
    o.Pipelines["db-read"] = new PipelineOptions
    {
        Retry          = new RetryOptions          { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(100) },
        CircuitBreaker = new CircuitBreakerOptions { FailureRatio = 0.5, MinimumThroughput = 5 },
    };

    // Payment charges — never retry; circuit breaks on sustained failures
    o.Pipelines["payment"] = new PipelineOptions
    {
        CircuitBreaker = new CircuitBreakerOptions { FailureRatio = 0.3, MinimumThroughput = 3 },
        Timeout        = new TimeoutOptions        { Timeout = TimeSpan.FromSeconds(15) },
    };
});
```

### Option B — `AddPrimitivesResiliencePipeline` (per feature module)

Each call adds one pipeline to the same `ResilienceOptions`. Useful when pipelines are defined
close to the feature that uses them:

```csharp
// In your HttpClient module
services.AddPrimitivesResiliencePipeline("http", o =>
{
    o.Retry   = new RetryOptions   { MaxAttempts = 3 };
    o.Timeout = new TimeoutOptions { Timeout = TimeSpan.FromSeconds(5) };
});

// In your data module
services.AddPrimitivesResiliencePipeline("db-read", o =>
{
    o.Retry          = new RetryOptions          { MaxAttempts = 2 };
    o.CircuitBreaker = new CircuitBreakerOptions { MinimumThroughput = 5 };
});
```

---

## Using `IResiliencePipelineProvider`

Inject `IResiliencePipelineProvider` and call `Get(name)` (non-generic) or `Get<T>(name)`
(generic — returns a result) at the call site:

```csharp
// Non-generic — void operations
var pipeline = pipelines.Get("cleanup-job");
await pipeline.ExecuteAsync(ct => DoCleanupAsync(ct), cancellationToken);

// Generic — operations that return a value
var pipeline = pipelines.Get<Order>("db-read");
var order    = await pipeline.ExecuteAsync(ct => db.FindOrderAsync(id, ct), cancellationToken);
```

If no pipeline is registered under the requested name, `Get` returns
`ResiliencePipeline[<T>].Empty` — an inert pipeline that executes the delegate without any
resilience wrapping — rather than throwing.

---

## Testing with named pipelines

In unit tests, register a no-strategy pipeline so the code under test executes without resilience
interference:

```csharp
var services = new ServiceCollection();
services.AddPrimitivesResilience(); // no pipelines — provider returns Empty for all names

// or register explicit passthrough pipelines
services.AddPrimitivesResiliencePipeline("payments", _ => { /* no strategies */ });
```

Or mock `IResiliencePipelineProvider` directly using NSubstitute or Moq:

```csharp
var provider = Substitute.For<IResiliencePipelineProvider>();
provider.Get<PaymentResult>("payments").Returns(ResiliencePipeline<PaymentResult>.Empty);
```

---

## Pipeline naming conventions

There is no enforced convention. Common patterns:

| Pattern | Example | Description |
|---------|---------|-------------|
| By downstream service | `"payment-api"`, `"user-db"` | One pipeline per external dependency |
| By operation type | `"db-read"`, `"db-write"` | Different retry behaviour for reads vs. writes |
| By sensitivity | `"critical"`, `"best-effort"` | Tiered policies shared across services |
