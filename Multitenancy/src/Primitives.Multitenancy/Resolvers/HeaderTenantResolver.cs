using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Resolvers;

/// <summary>Options for <see cref="HeaderTenantResolver"/>.</summary>
public sealed class HeaderResolverOptions
{
    /// <summary>HTTP header name to read the tenant identifier from. Defaults to <c>X-Tenant-Id</c>.</summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";
}

/// <summary>Resolves the tenant from an HTTP request header (default: <c>X-Tenant-Id</c>).</summary>
public sealed class HeaderTenantResolver : ITenantResolverStrategy
{
    private readonly HeaderResolverOptions _options;

    public HeaderTenantResolver(HeaderResolverOptions? options = null)
        => _options = options ?? new HeaderResolverOptions();

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var value = context.Request.Headers[_options.HeaderName].FirstOrDefault();
        return Task.FromResult(string.IsNullOrWhiteSpace(value) ? null : value);
    }
}
