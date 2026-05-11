---
layout: default
library: resilience
title: Installation
description: Add Primitives.Resilience to your .NET 8 project and register named pipelines.
permalink: /resilience/getting-started/
---

## Requirements

- .NET 8 or later
- `Primitives.Resilience` NuGet package (includes Polly v8)

## Install

```bash
dotnet add package Primitives.Resilience
```

## Register all pipelines at once

Use `AddPrimitivesResilience` to define the full set of named pipelines in one call:

```csharp
builder.Services.AddPrimitivesResilience(o =>
{
    o.Pipelines["http-client"] = new PipelineOptions
    {
        Retry   = new RetryOptions   { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(200) },
        Timeout = new TimeoutOptions { Timeout = TimeSpan.FromSeconds(5) },
    };

    o.Pipelines["database"] = new PipelineOptions
    {
        Retry          = new RetryOptions          { MaxAttempts = 2 },
        CircuitBreaker = new CircuitBreakerOptions { FailureRatio = 0.5, MinimumThroughput = 5 },
    };
});
```

## Register pipelines individually

Use `AddPrimitivesResiliencePipeline` to build up pipelines across multiple registrations or
feature modules:

```csharp
builder.Services
    .AddPrimitivesResiliencePipeline("http-client", o =>
    {
        o.Retry   = new RetryOptions   { MaxAttempts = 3 };
        o.Timeout = new TimeoutOptions { Timeout = TimeSpan.FromSeconds(5) };
    })
    .AddPrimitivesResiliencePipeline("database", o =>
    {
        o.Retry          = new RetryOptions          { MaxAttempts = 2 };
        o.CircuitBreaker = new CircuitBreakerOptions { MinimumThroughput = 5 };
    });
```

Both styles register the same `IResiliencePipelineProvider` singleton and can be mixed freely.

## Inject and use

```csharp
public class ProductService(IResiliencePipelineProvider pipelines, ProductRepository repo)
{
    public Task<Product?> GetAsync(int id, CancellationToken ct) =>
        pipelines.Get<Product?>("database")
                 .ExecuteAsync(ct => repo.FindAsync(id, ct), ct);
}
```

## Next steps

- [Strategies]({{ '/resilience/strategies/' | relative_url }}) — retry, circuit breaker, timeout, bulkhead
- [Named Pipelines]({{ '/resilience/pipelines/' | relative_url }}) — per-service pipelines, combining strategies
- [Configuration Reference]({{ '/resilience/reference/' | relative_url }}) — all options tables
