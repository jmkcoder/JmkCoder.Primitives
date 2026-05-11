---
layout: default
library: observability
permalink: /observability/
---

<div class="bd-hero">
  <h1>Primitives.Observability</h1>
  <p class="lead">
    OpenTelemetry-based observability for .NET 8 — distributed tracing, metrics, and structured
    logging with a single fluent DI registration. Bring your own exporter and logging backend.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Observability
  </div>
</div>

## The three pillars

**Tracing** tracks a request across services as a tree of spans. Each span records timing,
status, and attributes. Traces let you answer: *where did the latency come from?*

**Metrics** aggregate numbers over time — request rate, error rate, latency percentiles,
queue depth. Metrics let you answer: *is the system healthy right now?*

**Logging** captures discrete events with structured context. Logs let you answer:
*what exactly happened in this request?*

`Primitives.Observability` wires all three into the OpenTelemetry SDK with a single call.

## Quick start

```csharp
// Program.cs
builder.Services
    .AddPrimitivesObservability(o =>
    {
        o.ServiceName    = "order-api";
        o.ServiceVersion = "2.1.0";
        o.Environment    = builder.Environment.EnvironmentName;
    })
    .WithAspNetCoreInstrumentation()        // HTTP request tracing + RED metrics
    .WithHttpClientInstrumentation()        // outbound HttpClient tracing
    .AddActivitySource("Orders")            // register your custom ActivitySource
    .WithSerilog(log => log.WriteTo.Console())
    .WithOtlpExporter("http://localhost:4317");
```

```csharp
// Create spans in your services
public class OrderService(IActivitySourceProvider tracing)
{
    private readonly ActivitySource _src = tracing.GetSource("Orders");

    public async Task ProcessAsync(Order order, CancellationToken ct)
    {
        using var activity = _src.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", order.Id);
        // ...
    }
}
```

## Packages

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Observability</div>
    <p>Core package — OpenTelemetry SDK setup, <code>IActivitySourceProvider</code>, ASP.NET Core and HttpClient instrumentation, console and OTLP exporters.</p>
    <div class="install-cmd">dotnet add package Primitives.Observability</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Observability.Serilog</div>
    <p>Replaces the default logging providers with Serilog — structured log enrichment, filtering, and sink configuration (including the OpenTelemetry OTLP sink).</p>
    <div class="install-cmd">dotnet add package Primitives.Observability.Serilog</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Observability.Log4Net</div>
    <p>Wires log4net into <code>Microsoft.Extensions.Logging</code> — rolling files, SMTP, custom appenders — alongside OTel traces and metrics.</p>
    <div class="install-cmd">dotnet add package Primitives.Observability.Log4Net</div>
  </div>
</div>

## How it works

```
IServiceCollection.AddPrimitivesObservability()
    │
    ├─ ConfigureResource  → service.name / service.version / deployment.environment
    ├─ IActivitySourceProvider registered (singleton) — call .GetSource("Name")
    │
    ├─ .WithAspNetCoreInstrumentation()  → TracerProvider + MeterProvider auto-instrumented
    ├─ .WithHttpClientInstrumentation()  → HttpClient spans + metrics
    ├─ .AddActivitySource("Orders")      → OTel listens to your ActivitySource
    │
    ├─ .WithSerilog(…)   → ILoggerFactory backed by Serilog
    ├─ .WithLog4Net(…)   → log4net ILoggerProvider added
    │
    └─ .WithOtlpExporter("http://localhost:4317")
           └─ TracerProvider + MeterProvider → OTLP gRPC → Grafana / Jaeger / Honeycomb
```
