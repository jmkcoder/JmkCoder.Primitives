using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Resilience.Abstractions;
using Primitives.Resilience.Internal;
using Primitives.Resilience.Models;

namespace Primitives.Resilience.Extensions;

/// <summary>Extension methods for registering Primitives.Resilience with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Primitives.Resilience and all named pipelines defined in
    /// <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Optional delegate to populate <see cref="ResilienceOptions.Pipelines"/>.
    /// </param>
    public static IServiceCollection AddPrimitivesResilience(
        this IServiceCollection services,
        Action<ResilienceOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<ResilienceOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IResiliencePipelineProvider, PollyResiliencePipelineProvider>();
        return services;
    }

    /// <summary>
    /// Registers a single named resilience pipeline. Can be called multiple times to build
    /// up a set of pipelines incrementally. Works independently of
    /// <see cref="AddPrimitivesResilience"/> — calling either registers the provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The pipeline name used to retrieve it via
    /// <see cref="IResiliencePipelineProvider.Get(string)"/>.</param>
    /// <param name="configure">Delegate to configure the pipeline strategies.</param>
    public static IServiceCollection AddPrimitivesResiliencePipeline(
        this IServiceCollection services,
        string name,
        Action<PipelineOptions> configure)
    {
        services.AddLogging();
        services.Configure<ResilienceOptions>(o =>
        {
            var opts = new PipelineOptions();
            configure(opts);
            o.Pipelines[name] = opts;
        });
        services.TryAddSingleton<IResiliencePipelineProvider, PollyResiliencePipelineProvider>();
        return services;
    }
}
