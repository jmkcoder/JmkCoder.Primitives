using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Resolvers;

/// <summary>Options for <see cref="QueryStringTenantResolver"/>.</summary>
public sealed class QueryStringResolverOptions
{
    /// <summary>Query string parameter name. Defaults to <c>tenantId</c>.</summary>
    public string ParameterName { get; set; } = "tenantId";
}

/// <summary>
/// Resolves the tenant from a query string parameter (e.g. <c>?tenantId=acme</c>).
/// Suitable for development/testing. Not recommended for production APIs.
/// </summary>
public sealed class QueryStringTenantResolver : ITenantResolverStrategy
{
    private readonly QueryStringResolverOptions _options;

    public QueryStringTenantResolver(QueryStringResolverOptions? options = null)
        => _options = options ?? new QueryStringResolverOptions();

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var value = context.Request.Query[_options.ParameterName].FirstOrDefault();
        return Task.FromResult(string.IsNullOrWhiteSpace(value) ? null : value);
    }
}
