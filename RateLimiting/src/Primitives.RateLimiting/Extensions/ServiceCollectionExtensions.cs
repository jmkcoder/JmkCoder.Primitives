using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.RateLimiting.Abstractions;
using Primitives.RateLimiting.Internal;
using Primitives.RateLimiting.Middleware;

namespace Primitives.RateLimiting.Extensions;

/// <summary>Extension methods for registering and using the rate-limiting module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IRateLimiter"/> and <see cref="IRateLimitStore"/> (in-memory).
    /// </summary>
    /// <remarks>
    /// Define at least one policy via <paramref name="configure"/>:
    /// <code>
    /// services.AddPrimitivesRateLimiting(opts =>
    ///     opts.Policies.Add(new RateLimitPolicy { Name = "api", PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
    /// </code>
    /// Replace the store for distributed deployments via <c>.AddRateLimitStore&lt;TStore&gt;()</c>.
    /// </remarks>
    public static RateLimitingBuilder AddPrimitivesRateLimiting(
        this IServiceCollection services,
        Action<RateLimitingOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<RateLimitingOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
        services.TryAddSingleton<IRateLimiter, RateLimiter>();
        services.TryAddSingleton<IRateLimitKeyProvider, RemoteIpKeyProvider>();
        return new RateLimitingBuilder(services);
    }

    /// <summary>Replaces the default <see cref="IRateLimitStore"/> with a custom implementation.</summary>
    public static RateLimitingBuilder AddRateLimitStore<TStore>(this RateLimitingBuilder builder)
        where TStore : class, IRateLimitStore
    {
        builder.Services.RemoveAll<IRateLimitStore>();
        builder.Services.AddSingleton<IRateLimitStore, TStore>();
        return builder;
    }

    /// <summary>Replaces the default <see cref="IRateLimitKeyProvider"/> with a custom implementation.</summary>
    public static RateLimitingBuilder AddKeyProvider<TProvider>(this RateLimitingBuilder builder)
        where TProvider : class, IRateLimitKeyProvider
    {
        builder.Services.RemoveAll<IRateLimitKeyProvider>();
        builder.Services.AddSingleton<IRateLimitKeyProvider, TProvider>();
        return builder;
    }
}

/// <summary>Extension methods for adding the rate-limiting middleware to the pipeline.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the rate-limiting middleware for the named <paramref name="policy"/> to the pipeline.
    /// </summary>
    public static IApplicationBuilder UsePrimitivesRateLimiting(this IApplicationBuilder app, string policy = "default")
        => app.UseMiddleware<RateLimitingMiddleware>(policy);
}
