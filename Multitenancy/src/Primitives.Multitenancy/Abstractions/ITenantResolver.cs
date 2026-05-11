using Microsoft.AspNetCore.Http;

namespace Primitives.Multitenancy.Abstractions;

/// <summary>
/// Resolves the tenant identifier from the current <see cref="HttpContext"/>.
/// Returns <see langword="null"/> when this resolver cannot determine a tenant.
/// </summary>
public interface ITenantResolver
{
    Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default);
}

/// <summary>
/// Marker interface for resolver strategies registered in the composite resolver.
/// Implement this (instead of <see cref="ITenantResolver"/> directly) when
/// registering via <c>.WithXxxResolver()</c> fluent methods so the
/// <see cref="Resolvers.CompositeTenantResolver"/> can discover all strategies.
/// </summary>
public interface ITenantResolverStrategy : ITenantResolver { }
