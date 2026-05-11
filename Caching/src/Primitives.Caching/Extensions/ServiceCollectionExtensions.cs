using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Caching.Abstractions;
using Primitives.Caching.Providers;

namespace Primitives.Caching.Extensions;

/// <summary>Extension methods for registering Primitives.Caching with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICacheService"/> backed by <see cref="IMemoryCache"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="CacheOptions"/>.</param>
    public static IServiceCollection AddPrimitivesCache(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        services.AddMemoryCache();
        services.Configure<CacheOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<ICacheService, MemoryCacheService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="ICacheService"/> backed by the already-registered
    /// <see cref="IDistributedCache"/> (e.g. SQL Server, NCache, Cosmos, etc.).
    /// Call <c>AddDistributedXxx()</c> before calling this method.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="CacheOptions"/>.</param>
    public static IServiceCollection AddPrimitivesCacheDistributed(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        services.Configure<CacheOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<ICacheService, DistributedCacheService>();
        return services;
    }
}
