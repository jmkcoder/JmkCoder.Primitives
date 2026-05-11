using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Resolvers;

/// <summary>Options for <see cref="RouteValueTenantResolver"/>.</summary>
public sealed class RouteValueResolverOptions
{
    /// <summary>Route parameter name that contains the tenant identifier. Defaults to <c>tenantId</c>.</summary>
    public string RouteParameter { get; set; } = "tenantId";
}

/// <summary>
/// Resolves the tenant from a route value (e.g. <c>{tenantId}</c> in the route template).
/// Must be placed after routing middleware (<c>app.UseRouting()</c>) in the pipeline.
/// </summary>
public sealed class RouteValueTenantResolver : ITenantResolverStrategy
{
    private readonly RouteValueResolverOptions _options;

    public RouteValueTenantResolver(RouteValueResolverOptions? options = null)
        => _options = options ?? new RouteValueResolverOptions();

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        context.Request.RouteValues.TryGetValue(_options.RouteParameter, out var value);
        var str = value?.ToString();
        return Task.FromResult(string.IsNullOrWhiteSpace(str) ? null : str);
    }
}
