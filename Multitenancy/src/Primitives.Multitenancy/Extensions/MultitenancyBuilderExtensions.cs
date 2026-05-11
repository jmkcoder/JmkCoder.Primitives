using Microsoft.Extensions.DependencyInjection;
using Primitives.Multitenancy.Abstractions;
using Primitives.Multitenancy.Models;
using Primitives.Multitenancy.Resolvers;

namespace Primitives.Multitenancy.Extensions;

/// <summary>
/// Fluent extensions on <see cref="MultitenancyBuilder"/> for registering resolver strategies
/// and seeding the in-memory tenant store.
/// </summary>
public static class MultitenancyBuilderExtensions
{
    /// <summary>Adds <see cref="HostTenantResolver"/> as a resolver strategy.</summary>
    public static MultitenancyBuilder WithHostResolver(
        this MultitenancyBuilder builder,
        Action<HostResolverOptions>? configure = null)
    {
        var opts = new HostResolverOptions();
        configure?.Invoke(opts);
        builder.Services.AddSingleton<ITenantResolverStrategy>(new HostTenantResolver(opts));
        return builder;
    }

    /// <summary>
    /// Adds <see cref="HeaderTenantResolver"/> as a resolver strategy.
    /// Defaults to reading the <c>X-Tenant-Id</c> header.
    /// </summary>
    public static MultitenancyBuilder WithHeaderResolver(
        this MultitenancyBuilder builder,
        string headerName = "X-Tenant-Id")
    {
        builder.Services.AddSingleton<ITenantResolverStrategy>(
            new HeaderTenantResolver(new HeaderResolverOptions { HeaderName = headerName }));
        return builder;
    }

    /// <summary>
    /// Adds <see cref="RouteValueTenantResolver"/> as a resolver strategy.
    /// Must be placed after <c>app.UseRouting()</c>.
    /// </summary>
    public static MultitenancyBuilder WithRouteValueResolver(
        this MultitenancyBuilder builder,
        string routeParameter = "tenantId")
    {
        builder.Services.AddSingleton<ITenantResolverStrategy>(
            new RouteValueTenantResolver(new RouteValueResolverOptions { RouteParameter = routeParameter }));
        return builder;
    }

    /// <summary>
    /// Adds <see cref="ClaimTenantResolver"/> as a resolver strategy.
    /// Requires authentication middleware to populate <c>HttpContext.User</c> before tenant resolution.
    /// </summary>
    public static MultitenancyBuilder WithClaimResolver(
        this MultitenancyBuilder builder,
        string claimType = "tenant_id")
    {
        builder.Services.AddSingleton<ITenantResolverStrategy>(
            new ClaimTenantResolver(new ClaimResolverOptions { ClaimType = claimType }));
        return builder;
    }

    /// <summary>
    /// Adds <see cref="QueryStringTenantResolver"/> as a resolver strategy.
    /// Suitable for development. Not recommended in production APIs.
    /// </summary>
    public static MultitenancyBuilder WithQueryStringResolver(
        this MultitenancyBuilder builder,
        string parameterName = "tenantId")
    {
        builder.Services.AddSingleton<ITenantResolverStrategy>(
            new QueryStringTenantResolver(new QueryStringResolverOptions { ParameterName = parameterName }));
        return builder;
    }

    /// <summary>
    /// Seeds the in-memory tenant store with the provided tenants.
    /// Appends to any tenants already configured in <see cref="MultitenancyOptions"/>.
    /// </summary>
    public static MultitenancyBuilder WithInMemoryTenants(
        this MultitenancyBuilder builder,
        Action<List<Tenant>> configureTenants)
    {
        builder.Services.Configure<MultitenancyOptions>(o => configureTenants(o.Tenants));
        return builder;
    }
}
