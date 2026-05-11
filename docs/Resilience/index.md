---
layout: default
library: resilience
permalink: /resilience/
---

<div class="bd-hero">
  <h1>Primitives.Resilience</h1>
  <p class="lead">
    Circuit breakers, retry policies, bulkheads, and timeouts built on Polly&nbsp;v8
    — pre-wired for .NET DI with zero boilerplate. Configure named pipelines once;
    inject <code>IResiliencePipelineProvider</code> anywhere.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Resilience
  </div>
</div>

## The problem it solves

Polly v8 is powerful, but wiring it into .NET DI involves verbose builder calls, custom extension
methods, and `ResiliencePipelineProvider<string>` leaking into application code. Teams end up
copy-pasting the same setup across projects.

`Primitives.Resilience` gives you:

- **One interface** — `IResiliencePipelineProvider` — with `Get(name)` and `Get<T>(name)`.
- **Options-based configuration** — define pipelines as plain C# objects (or from `appsettings.json`).
- **Zero Polly boilerplate** — all `ResiliencePipelineBuilder` calls happen inside the library.
- **All four strategies** — retry, circuit breaker, timeout, and bulkhead in every pipeline.

## Quick start

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

## Package

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Resilience</div>
    <p>Named pipeline configuration, <code>IResiliencePipelineProvider</code>, and DI registration over Polly v8.</p>
    <div class="install-cmd">dotnet add package Primitives.Resilience</div>
  </div>
</div>

## Design principles

**Named pipelines, not scattered policies.** Define all resilience behaviour in one place at
startup, then look up a pipeline by name at the call site. Changing retry counts or circuit-breaker
thresholds is a single configuration change.

**Options over builders.** `PipelineOptions` is a plain C# object — no builder chain required.
It binds naturally from `IConfiguration` for environment-specific settings.

**Strategy composition.** Enable only the strategies you need. Each property on `PipelineOptions`
is nullable — omit it to skip that strategy entirely.

**Polly under the hood, not in your face.** Application code depends on
`IResiliencePipelineProvider` and `ResiliencePipeline<T>` (Polly's public contract). The DI wiring
is inside the library.
