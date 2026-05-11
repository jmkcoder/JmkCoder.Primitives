---
layout: default
library: observability
title: Configuration Reference
description: Full API reference for Primitives.Observability — options, interfaces, and builder methods.
permalink: /observability/reference/
---

## `ObservabilityOptions`

Configure via `AddPrimitivesObservability(o => { … })`:

| Property | Type | Default | OTel attribute |
|----------|------|---------|----------------|
| `ServiceName` | `string` | `"unknown-service"` | `service.name` |
| `ServiceVersion` | `string` | `"1.0.0"` | `service.version` |
| `Environment` | `string` | `"production"` | `deployment.environment` |

---

## `AddPrimitivesObservability`

```csharp
IServiceCollection.AddPrimitivesObservability(
    Action<ObservabilityOptions>? configure = null)
    → ObservabilityBuilder
```

Registers:
- OpenTelemetry SDK (`AddOpenTelemetry()`) with the service resource populated from `ObservabilityOptions`
- `IActivitySourceProvider` (singleton) — `ActivitySourceProvider`
- `IOptions<ObservabilityOptions>`

---

## `ObservabilityBuilder`

| Member | Type | Description |
|--------|------|-------------|
| `Services` | `IServiceCollection` | Underlying service collection |
| `OpenTelemetryBuilder` | `OpenTelemetryBuilder` | Access for advanced OTel configuration |

---

## `ObservabilityBuilderExtensions`

### Tracing and metrics

```csharp
// Pass custom config to the OTel TracerProviderBuilder
.WithTracing(Action<TracerProviderBuilder>? configure = null)

// Pass custom config to the OTel MeterProviderBuilder
.WithMetrics(Action<MeterProviderBuilder>? configure = null)

// Register ActivitySource names so OTel captures their spans
.AddActivitySource(params string[] sourceNames)
```

### Instrumentation

```csharp
// ASP.NET Core request tracing + RED metrics (requests, errors, duration)
.WithAspNetCoreInstrumentation()

// Outbound HttpClient request tracing + metrics
.WithHttpClientInstrumentation()
```

### Exporters

```csharp
// Console exporter — traces + metrics to stdout (development only)
.WithConsoleExporter()

// OTLP exporter — traces + metrics via gRPC (defaults to http://localhost:4317)
.WithOtlpExporter(string? endpoint = null)
```

---

## `IActivitySourceProvider`

```csharp
public interface IActivitySourceProvider
{
    ActivitySource GetSource(string name);
}
```

Returns a singleton `ActivitySource` keyed by name. Multiple calls with the same name return
the same instance. The source's `Version` is set from `ObservabilityOptions.ServiceVersion`.

**Usage:**

```csharp
public class OrderService(IActivitySourceProvider tracing)
{
    private readonly ActivitySource _src = tracing.GetSource("Orders");

    public async Task ProcessAsync(Order order)
    {
        using var activity = _src.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", order.Id);
        // ...
    }
}
```

> Must also call `.AddActivitySource("Orders")` during bootstrap for spans to be captured.

---

## Serilog — `WithSerilog`

```csharp
// Extension on ObservabilityBuilder from Primitives.Observability.Serilog
ObservabilityBuilder.WithSerilog(
    Action<LoggerConfiguration>? configure = null,
    bool clearExistingProviders = true)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `configure` | `null` | Delegate to set sinks, enrichers, minimum levels |
| `clearExistingProviders` | `true` | Clear existing `ILoggerProvider`s before adding Serilog |

Sets `Serilog.Log.Logger` and registers Serilog as the `ILoggerFactory` backend.

---

## Log4Net — `WithLog4Net`

```csharp
// Extension on ObservabilityBuilder from Primitives.Observability.Log4Net
ObservabilityBuilder.WithLog4Net(string configFile = "log4net.config")
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `configFile` | `"log4net.config"` | Path to the log4net XML configuration file |

Adds `Log4NetProvider` via `Microsoft.Extensions.Logging.Log4Net.AspNetCore`. Does **not** clear
existing providers — log4net runs alongside them.

---

## Middleware placement

`Primitives.Observability` does not add ASP.NET Core middleware — all configuration is done at
the DI level. The OTel SDK's hosted services start automatically when the generic host starts.

For ASP.NET Core instrumentation to trace requests, ensure `WithAspNetCoreInstrumentation()` is
called before `app.MapControllers()` / `app.MapEndpoints()`. No explicit `app.Use…` call is needed.
