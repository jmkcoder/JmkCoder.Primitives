using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;

namespace Primitives.Observability.Extensions;

/// <summary>
/// Fluent builder returned by <c>AddPrimitivesObservability()</c>.
/// Chain <c>.With*()</c> methods to configure tracing, metrics, logging, exporters,
/// and instrumentation. Access <see cref="OpenTelemetryBuilder"/> directly for
/// advanced scenarios not covered by the extension methods.
/// </summary>
public sealed class ObservabilityBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// The underlying OpenTelemetry builder.
    /// Use this for advanced configuration not exposed by the fluent API.
    /// </summary>
    public OpenTelemetryBuilder OpenTelemetryBuilder { get; }

    internal ObservabilityBuilder(IServiceCollection services, OpenTelemetryBuilder otelBuilder)
    {
        Services = services;
        OpenTelemetryBuilder = otelBuilder;
    }
}
