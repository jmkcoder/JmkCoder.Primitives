using Microsoft.Extensions.DependencyInjection;

namespace Primitives.Multitenancy.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesMultitenancy"/>.</summary>
public sealed class MultitenancyBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal MultitenancyBuilder(IServiceCollection services)
        => Services = services;
}
