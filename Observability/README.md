# Primitives.Observability

OpenTelemetry-based observability for .NET 8 — distributed tracing, metrics, and structured logging with a single fluent DI registration.

## Packages

| Package | Description |
|---------|-------------|
| `Primitives.Observability` | Core — OTel SDK setup, `IActivitySourceProvider`, instrumentation, exporters |
| `Primitives.Observability.Serilog` | Serilog backend for structured log ingestion |
| `Primitives.Observability.Log4Net` | Log4Net backend via `Microsoft.Extensions.Logging` |

## Quick start

```bash
dotnet add package Primitives.Observability
dotnet add package Primitives.Observability.Serilog   # optional
dotnet add package Primitives.Observability.Log4Net   # optional
```

```csharp
// Program.cs
builder.Services
    .AddPrimitivesObservability(o =>
    {
        o.ServiceName    = "order-api";
        o.ServiceVersion = "2.1.0";
        o.Environment    = builder.Environment.EnvironmentName;
    })
    .WithAspNetCoreInstrumentation()   // traces + metrics for HTTP requests
    .WithHttpClientInstrumentation()   // traces + metrics for outbound HttpClient calls
    .AddActivitySource("Orders")       // register your custom ActivitySource
    .WithSerilog(log => log            // structured logging via Serilog
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(o =>    // Serilog.Sinks.OpenTelemetry (separate package)
        {
            o.Endpoint = "http://localhost:4317";
        }))
    .WithOtlpExporter("http://localhost:4317");  // traces + metrics → OTLP
```

## Tracing your own code

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

Register the source so OTel captures its spans:

```csharp
.AddActivitySource("Orders")
```

## Exporters

| Method | Output | Use case |
|--------|--------|----------|
| `.WithConsoleExporter()` | stdout | Development |
| `.WithOtlpExporter(endpoint?)` | gRPC OTLP | Production (Grafana, Jaeger, Honeycomb, …) |

## Logging backends

### Serilog

```csharp
.WithSerilog(log => log
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithMachineName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.OpenTelemetry(o =>   // from Serilog.Sinks.OpenTelemetry
    {
        o.Endpoint = "http://localhost:4317";
        o.Protocol = OtlpProtocol.Grpc;
    }))
```

`Serilog.Sinks.OpenTelemetry` ships logs via OTLP independently of the tracing/metrics SDK,
making each signal configurable and independently scalable.

### Log4Net

```csharp
.WithLog4Net("log4net.config")   // default: "log4net.config"
```

```xml
<!-- log4net.config -->
<log4net>
  <appender name="RollingFile" type="log4net.Appender.RollingFileAppender">
    <file value="logs/app.log" />
    <appendToFile value="true" />
    <rollingStyle value="Date" />
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
    </layout>
  </appender>
  <root>
    <level value="INFO" />
    <appender-ref ref="RollingFile" />
  </root>
</log4net>
```

Log4Net adds to existing providers; Serilog replaces them by default (set `clearExistingProviders: false` to change this).

## License

MIT
