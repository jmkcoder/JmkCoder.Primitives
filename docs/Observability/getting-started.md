---
layout: default
library: observability
title: Installation
description: Add Primitives.Observability to a .NET 8 application and configure tracing, metrics, and logging.
permalink: /observability/getting-started/
---

## Requirements

- .NET 8 or later
- An OTLP-compatible backend (Grafana, Jaeger, Honeycomb, OpenTelemetry Collector, etc.) — or the console exporter for development

## Install

```bash
# Core — always required
dotnet add package Primitives.Observability

# Optional logging backends (pick one or both)
dotnet add package Primitives.Observability.Serilog
dotnet add package Primitives.Observability.Log4Net
```

## Minimal setup (console exporter)

The fastest way to get started during development:

```csharp
// Program.cs
builder.Services
    .AddPrimitivesObservability(o => o.ServiceName = "my-api")
    .WithAspNetCoreInstrumentation()
    .WithConsoleExporter();   // prints spans and metrics to stdout
```

## Production setup (OTLP exporter + Serilog)

```csharp
builder.Services
    .AddPrimitivesObservability(o =>
    {
        o.ServiceName    = builder.Configuration["Service:Name"]!;
        o.ServiceVersion = builder.Configuration["Service:Version"]!;
        o.Environment    = builder.Environment.EnvironmentName;
    })
    .WithAspNetCoreInstrumentation()
    .WithHttpClientInstrumentation()
    .AddActivitySource("Orders", "Payments")    // register your ActivitySources
    .WithSerilog(log => log
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service.name", builder.Configuration["Service:Name"])
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(o =>              // from Serilog.Sinks.OpenTelemetry
        {
            o.Endpoint = builder.Configuration["Otlp:Endpoint"];
            o.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
        }))
    .WithOtlpExporter(builder.Configuration["Otlp:Endpoint"]);
```

`appsettings.json`:

```json
{
  "Service": {
    "Name":    "order-api",
    "Version": "2.1.0"
  },
  "Otlp": {
    "Endpoint": "http://otel-collector:4317"
  }
}
```

## Tracing your own code

1. Register your source name at startup:

```csharp
.AddActivitySource("Orders")
```

2. Inject `IActivitySourceProvider` and create spans:

```csharp
public class OrderService(IActivitySourceProvider tracing, ILogger<OrderService> logger)
{
    private readonly ActivitySource _src = tracing.GetSource("Orders");

    public async Task<Guid> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        using var activity = _src.StartActivity("PlaceOrder");
        activity?.SetTag("customer.id", request.CustomerId);

        logger.LogInformation("Placing order for customer {CustomerId}", request.CustomerId);

        var orderId = await _repository.InsertAsync(request.ToOrder(), ct);

        activity?.SetTag("order.id", orderId);
        return orderId;
    }
}
```

Spans that are not registered with `.AddActivitySource()` are created but **not sampled** — they
cost nothing unless a listener is attached.

## Next steps

- [Exporters]({{ '/observability/exporters/' | relative_url }}) — OTLP, console, custom
- [Logging]({{ '/observability/logging/' | relative_url }}) — Serilog and Log4Net configuration
- [Configuration Reference]({{ '/observability/reference/' | relative_url }}) — all options
