---
layout: default
library: observability
title: Logging
description: Serilog and Log4Net integration with Primitives.Observability.
permalink: /observability/logging/
---

## Overview

`Primitives.Observability` is opinionated about _tracing_ and _metrics_ (OpenTelemetry SDK),
but neutral about _logging_ — you choose the backend. Two ready-made integrations are provided:

| Package | Backend | Approach |
|---------|---------|----------|
| `Primitives.Observability.Serilog` | Serilog | Replaces `ILoggerFactory` with Serilog |
| `Primitives.Observability.Log4Net` | Log4Net | Adds a `Log4NetProvider` to existing providers |

---

## Serilog

### Install

```bash
dotnet add package Primitives.Observability.Serilog
dotnet add package Serilog.Sinks.OpenTelemetry   # for OTLP log shipping (optional)
dotnet add package Serilog.Sinks.File            # for file logging (optional)
```

### Basic setup

```csharp
.WithSerilog(log => log
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console())
```

### OTLP log shipping

```csharp
.WithSerilog(log => log
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.OpenTelemetry(o =>
    {
        o.Endpoint = "http://localhost:4317";
        o.Protocol = OtlpProtocol.Grpc;
        o.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"]    = "order-api",
            ["service.version"] = "2.1.0",
        };
    }))
```

### File + console

```csharp
.WithSerilog(log => log
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System",    LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        path:            "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14))
```

### Keeping existing providers

By default `.WithSerilog()` clears the built-in providers (console, debug) so that Serilog is
the only log sink. Pass `clearExistingProviders: false` to add Serilog alongside them:

```csharp
.WithSerilog(configure: log => log.WriteTo.Console(),
             clearExistingProviders: false)
```

### Trace correlation

When using OTel tracing alongside Serilog, log entries automatically carry the current trace and
span IDs via `Activity.Current`. To include them in your output, use the Serilog enricher from
`Serilog.Enrichers.Span` (third-party) or read from `Activity.Current` manually:

```csharp
public class OrderService(ILogger<OrderService> logger)
{
    public Task ProcessAsync(Order order)
    {
        using var activity = _src.StartActivity("ProcessOrder");

        // Log entries written inside the activity automatically have the right
        // TraceId/SpanId when using Serilog.Sinks.OpenTelemetry.
        logger.LogInformation("Processing order {OrderId}", order.Id);
        return Task.CompletedTask;
    }
}
```

---

## Log4Net

### Install

```bash
dotnet add package Primitives.Observability.Log4Net
```

### Registration

```csharp
.WithLog4Net()                    // looks for "log4net.config" in the app directory
.WithLog4Net("config/log4net.xml")  // explicit path
```

### Configuration file

```xml
<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  <!-- Rolling file appender -->
  <appender name="RollingFile" type="log4net.Appender.RollingFileAppender">
    <file value="logs/app.log" />
    <appendToFile value="true" />
    <rollingStyle value="Composite" />
    <maxSizeRollBackups value="14" />
    <maximumFileSize value="50MB" />
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date{ISO8601} [%thread] %-5level %logger{1} - %message%newline" />
    </layout>
  </appender>

  <!-- Console appender -->
  <appender name="Console" type="log4net.Appender.ConsoleAppender">
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%-5level %logger{1} - %message%newline" />
    </layout>
  </appender>

  <root>
    <level value="INFO" />
    <appender-ref ref="RollingFile" />
    <appender-ref ref="Console" />
  </root>

  <!-- Suppress noisy Microsoft framework loggers -->
  <logger name="Microsoft">
    <level value="WARN" />
  </logger>
</log4net>
```

### Log4Net vs Serilog

| | Serilog | Log4Net |
|---|---------|---------|
| Provider behaviour | Replaces existing providers | Adds alongside existing providers |
| Structured data | First-class (`{@obj}` destructuring) | Via pattern conversion only |
| OTLP log shipping | Via `Serilog.Sinks.OpenTelemetry` | Not built-in |
| Config style | Code-first | XML file |
| Ecosystem | Rich sink library | Mature appender library |

Use **Serilog** when you want structured logs and OTLP log shipping in the same pipeline as
your traces. Use **Log4Net** when migrating an existing application with established log4net
infrastructure, or when you need log4net-specific appenders (database, SMTP, etc.).
