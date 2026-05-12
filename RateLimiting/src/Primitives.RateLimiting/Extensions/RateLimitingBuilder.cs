using Microsoft.Extensions.DependencyInjection;

namespace Primitives.RateLimiting.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesRateLimiting"/>.</summary>
public sealed class RateLimitingBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal RateLimitingBuilder(IServiceCollection services)
        => Services = services;
}
