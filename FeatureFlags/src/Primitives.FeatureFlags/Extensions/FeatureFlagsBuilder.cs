using Microsoft.Extensions.DependencyInjection;

namespace Primitives.FeatureFlags.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesFeatureFlags"/>.</summary>
public sealed class FeatureFlagsBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal FeatureFlagsBuilder(IServiceCollection services)
        => Services = services;
}
