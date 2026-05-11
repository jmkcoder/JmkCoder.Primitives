using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Multitenancy.Abstractions;
using Primitives.Multitenancy.Internal;
using Primitives.Multitenancy.Resolvers;

namespace Primitives.Multitenancy.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core multitenancy services: <see cref="ITenantResolver"/> (composite),
    /// <see cref="ITenantStore"/> (in-memory), and <see cref="ITenantAccessor"/>.
    /// </summary>
    /// <remarks>
    /// Chain <c>.WithHeaderResolver()</c>, <c>.WithHostResolver()</c>, etc. on the returned
    /// <see cref="MultitenancyBuilder"/> to register at least one resolver strategy.
    /// Call <c>app.UsePrimitivesMultitenancy()</c> in the middleware pipeline to activate resolution.
    /// </remarks>
    public static MultitenancyBuilder AddPrimitivesMultitenancy(
        this IServiceCollection services,
        Action<MultitenancyOptions>? configure = null)
    {
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.Configure<MultitenancyOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<ITenantResolver, CompositeTenantResolver>();
        services.TryAddSingleton<ITenantStore, InMemoryTenantStore>();
        services.TryAddScoped<ITenantAccessor, TenantAccessor>();
        return new MultitenancyBuilder(services);
    }

    /// <summary>Replaces the default <see cref="ITenantStore"/> with a custom implementation.</summary>
    public static MultitenancyBuilder AddTenantStore<TStore>(this MultitenancyBuilder builder)
        where TStore : class, ITenantStore
    {
        builder.Services.RemoveAll<ITenantStore>();
        builder.Services.AddSingleton<ITenantStore, TStore>();
        return builder;
    }
}
