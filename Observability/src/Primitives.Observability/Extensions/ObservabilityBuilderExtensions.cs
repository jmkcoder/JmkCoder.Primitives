using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Primitives.Observability.Extensions;

public static class ObservabilityBuilderExtensions
{
    /// <summary>
    /// Configures OpenTelemetry tracing via the provided delegate.
    /// </summary>
    public static ObservabilityBuilder WithTracing(
        this ObservabilityBuilder builder,
        Action<TracerProviderBuilder>? configure = null)
    {
        builder.OpenTelemetryBuilder.WithTracing(tracing => configure?.Invoke(tracing));
        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry metrics via the provided delegate.
    /// </summary>
    public static ObservabilityBuilder WithMetrics(
        this ObservabilityBuilder builder,
        Action<MeterProviderBuilder>? configure = null)
    {
        builder.OpenTelemetryBuilder.WithMetrics(metrics => configure?.Invoke(metrics));
        return builder;
    }

    /// <summary>
    /// Registers one or more <see cref="System.Diagnostics.ActivitySource"/> names with
    /// the tracer provider so that spans from those sources are captured.
    /// </summary>
    public static ObservabilityBuilder AddActivitySource(
        this ObservabilityBuilder builder,
        params string[] sourceNames)
    {
        builder.OpenTelemetryBuilder.WithTracing(t => t.AddSource(sourceNames));
        return builder;
    }

    /// <summary>
    /// Adds automatic tracing and metrics instrumentation for ASP.NET Core requests.
    /// </summary>
    public static ObservabilityBuilder WithAspNetCoreInstrumentation(
        this ObservabilityBuilder builder)
    {
        builder.OpenTelemetryBuilder
            .WithTracing(t => t.AddAspNetCoreInstrumentation())
            .WithMetrics(m => m.AddAspNetCoreInstrumentation());
        return builder;
    }

    /// <summary>
    /// Adds automatic tracing and metrics instrumentation for outbound <c>HttpClient</c> calls.
    /// </summary>
    public static ObservabilityBuilder WithHttpClientInstrumentation(
        this ObservabilityBuilder builder)
    {
        builder.OpenTelemetryBuilder
            .WithTracing(t => t.AddHttpClientInstrumentation())
            .WithMetrics(m => m.AddHttpClientInstrumentation());
        return builder;
    }

    /// <summary>
    /// Adds the console exporter for both traces and metrics.
    /// Intended for development — not recommended for production.
    /// </summary>
    public static ObservabilityBuilder WithConsoleExporter(
        this ObservabilityBuilder builder)
    {
        builder.OpenTelemetryBuilder
            .WithTracing(t => t.AddConsoleExporter())
            .WithMetrics(m => m.AddConsoleExporter());
        return builder;
    }

    /// <summary>
    /// Adds the OTLP exporter for both traces and metrics.
    /// Defaults to <c>http://localhost:4317</c> (gRPC) when <paramref name="endpoint"/> is <c>null</c>.
    /// </summary>
    public static ObservabilityBuilder WithOtlpExporter(
        this ObservabilityBuilder builder,
        string? endpoint = null)
    {
        void Configure(OtlpExporterOptions o)
        {
            if (endpoint is not null)
                o.Endpoint = new Uri(endpoint);
        }

        builder.OpenTelemetryBuilder
            .WithTracing(t => t.AddOtlpExporter(Configure))
            .WithMetrics(m => m.AddOtlpExporter(Configure));
        return builder;
    }
}
