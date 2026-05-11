using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Resources;
using Primitives.Observability.Abstractions;
using Primitives.Observability.Internal;

namespace Primitives.Observability.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry SDK infrastructure, <see cref="IActivitySourceProvider"/>,
    /// and the <see cref="ObservabilityOptions"/> resource attributes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to customise <see cref="ObservabilityOptions"/>.</param>
    /// <returns>A fluent <see cref="ObservabilityBuilder"/> for further configuration.</returns>
    public static ObservabilityBuilder AddPrimitivesObservability(
        this IServiceCollection services,
        Action<ObservabilityOptions>? configure = null)
    {
        var options = new ObservabilityOptions();
        configure?.Invoke(options);

        services.Configure<ObservabilityOptions>(o =>
        {
            o.ServiceName    = options.ServiceName;
            o.ServiceVersion = options.ServiceVersion;
            o.Environment    = options.Environment;
        });

        services.TryAddSingleton<IActivitySourceProvider, ActivitySourceProvider>();

        var otelBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName:    options.ServiceName,
                    serviceVersion: options.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = options.Environment,
                }));

        return new ObservabilityBuilder(services, otelBuilder);
    }
}
