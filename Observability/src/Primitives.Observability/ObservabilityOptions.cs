namespace Primitives.Observability;

/// <summary>
/// Top-level options for <c>AddPrimitivesObservability()</c>.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>
    /// Logical name that identifies this service in traces, metrics, and logs.
    /// Maps to the OpenTelemetry <c>service.name</c> resource attribute.
    /// </summary>
    public string ServiceName { get; set; } = "unknown-service";

    /// <summary>
    /// Semver-style version of the service.
    /// Maps to the OpenTelemetry <c>service.version</c> resource attribute.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Deployment environment (e.g. production, staging, development).
    /// Maps to the OpenTelemetry <c>deployment.environment</c> resource attribute.
    /// </summary>
    public string Environment { get; set; } = "production";
}
