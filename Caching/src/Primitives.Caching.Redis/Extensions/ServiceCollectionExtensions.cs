using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Caching.Abstractions;
using StackExchange.Redis;

namespace Primitives.Caching.Redis.Extensions;

/// <summary>Extension methods for registering the Redis cache provider.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICacheService"/> backed by Redis.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureRedis">Delegate to configure <see cref="RedisCacheOptions"/>.</param>
    /// <param name="configureCache">Optional delegate to configure <see cref="CacheOptions"/>.</param>
    public static IServiceCollection AddPrimitivesCacheRedis(
        this IServiceCollection services,
        Action<RedisCacheOptions> configureRedis,
        Action<CacheOptions>? configureCache = null)
    {
        services.Configure(configureRedis);
        services.Configure<CacheOptions>(configureCache ?? (_ => { }));

        // Register IConnectionMultiplexer if the caller hasn't already
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;

            if (opts.ConnectionMultiplexerFactory is not null)
                return opts.ConnectionMultiplexerFactory(sp);

            if (string.IsNullOrWhiteSpace(opts.Configuration))
                throw new InvalidOperationException(
                    "Either RedisCacheOptions.Configuration or RedisCacheOptions.ConnectionMultiplexerFactory must be set.");

            return ConnectionMultiplexer.Connect(opts.Configuration);
        });

        services.TryAddSingleton<ICacheService, RedisCacheService>();
        return services;
    }
}
