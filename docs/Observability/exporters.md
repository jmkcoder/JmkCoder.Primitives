---
layout: default
library: observability
title: Exporters
description: OTLP, console, and custom exporters for traces and metrics.
permalink: /observability/exporters/
---

## Console exporter

Prints spans and metrics to standard output. Zero infrastructure required.

```csharp
builder.Services
    .AddPrimitivesObservability(o => o.ServiceName = "my-api")
    .WithConsoleExporter();
```

Use exclusively in development — the output is verbose and unstructured.

---

## OTLP exporter (gRPC)

Exports spans and metrics via OpenTelemetry Protocol over gRPC to any OTLP-compatible backend.

```csharp
// Default endpoint: http://localhost:4317
.WithOtlpExporter()

// Custom endpoint
.WithOtlpExporter("http://otel-collector:4317")
```

Compatible backends include Grafana Tempo + Mimir, Jaeger, Zipkin (via Collector),
Honeycomb, Lightstep, Datadog Agent (OTLP intake), and any OpenTelemetry Collector deployment.

### With an OpenTelemetry Collector

The Collector is the recommended production topology — it buffers, retries, and fans out to
multiple backends:

```yaml
# docker-compose.yml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    ports:
      - "4317:4317"   # gRPC
      - "4318:4318"   # HTTP
    volumes:
      - ./otel-config.yaml:/etc/otelcol/config.yaml
```

```yaml
# otel-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317

exporters:
  jaeger:
    endpoint: jaeger:14250
  prometheus:
    endpoint: 0.0.0.0:8889

service:
  pipelines:
    traces:
      receivers: [otlp]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      exporters: [prometheus]
```

---

## Combining exporters

Chain multiple exporter calls — each adds to the same pipeline:

```csharp
.AddPrimitivesObservability(o => o.ServiceName = "my-api")
.WithAspNetCoreInstrumentation()
.WithConsoleExporter()                         // dev: see spans in terminal
.WithOtlpExporter("http://otel-collector:4317") // prod: ship to backend
```

---

## Advanced exporter configuration

For scenarios beyond the fluent API, access the underlying `OpenTelemetryBuilder` directly:

```csharp
var obsBuilder = builder.Services.AddPrimitivesObservability(o =>
{
    o.ServiceName = "my-api";
});

// Fine-grained control
obsBuilder.OpenTelemetryBuilder
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://otel-collector:4317");
            o.ExportProcessorType = ExportProcessorType.Batch;
            o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
            {
                MaxQueueSize        = 2048,
                ScheduledDelayMilliseconds = 5000,
                MaxExportBatchSize  = 512,
            };
        }))
    .WithMetrics(metrics => metrics
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")));
```

---

## Separating logs from traces and metrics

Serilog's OTLP sink ships logs as a separate signal, giving you independent control:

```csharp
// Traces + metrics → Jaeger / Grafana Tempo
.WithOtlpExporter("http://otel-collector:4317")

// Logs → Loki (different OTLP endpoint or backend)
.WithSerilog(log => log.WriteTo.OpenTelemetry(o =>
{
    o.Endpoint = "http://loki:3100/otlp";
    o.Protocol = OtlpProtocol.HttpProtobuf;
}))
```
