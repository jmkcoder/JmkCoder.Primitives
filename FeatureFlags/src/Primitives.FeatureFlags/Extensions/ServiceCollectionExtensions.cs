using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.FeatureFlags.Abstractions;
using Primitives.FeatureFlags.Internal;
using Primitives.FeatureFlags.Models;

namespace Primitives.FeatureFlags.Extensions;

/// <summary>Extension methods for registering the feature flags module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the feature flags services: <see cref="IFeatureFlagService"/> and
    /// <see cref="IFeatureFlagStore"/> (in-memory by default).
    /// </summary>
    /// <remarks>
    /// Seed flags inline via <paramref name="configure"/>:
    /// <code>
    /// services.AddPrimitivesFeatureFlags(opts =>
    ///     opts.Flags.Add(new FeatureFlag { Name = "new-ui", IsEnabled = true }));
    /// </code>
    /// Replace the store by calling <c>.AddFeatureFlagStore&lt;TStore&gt;()</c> on the returned builder.
    /// </remarks>
    public static FeatureFlagsBuilder AddPrimitivesFeatureFlags(
        this IServiceCollection services,
        Action<FeatureFlagsOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<FeatureFlagsOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IFeatureFlagStore, InMemoryFeatureFlagStore>();
        services.TryAddSingleton<IFeatureFlagService, FeatureFlagService>();

        // Seed configured flags into the in-memory store via hosted startup
        services.AddHostedService<FeatureFlagSeedService>();

        return new FeatureFlagsBuilder(services);
    }

    /// <summary>Replaces the default <see cref="IFeatureFlagStore"/> with a custom implementation.</summary>
    public static FeatureFlagsBuilder AddFeatureFlagStore<TStore>(this FeatureFlagsBuilder builder)
        where TStore : class, IFeatureFlagStore
    {
        builder.Services.RemoveAll<IFeatureFlagStore>();
        builder.Services.AddSingleton<IFeatureFlagStore, TStore>();
        return builder;
    }
}
