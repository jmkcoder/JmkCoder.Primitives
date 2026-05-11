using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Resolvers;

/// <summary>Options for <see cref="ClaimTenantResolver"/>.</summary>
public sealed class ClaimResolverOptions
{
    /// <summary>Claim type whose value is used as the tenant identifier. Defaults to <c>tenant_id</c>.</summary>
    public string ClaimType { get; set; } = "tenant_id";
}

/// <summary>
/// Resolves the tenant from an authenticated user claim.
/// Requires authentication middleware to run before tenant resolution.
/// </summary>
public sealed class ClaimTenantResolver : ITenantResolverStrategy
{
    private readonly ClaimResolverOptions _options;

    public ClaimTenantResolver(ClaimResolverOptions? options = null)
        => _options = options ?? new ClaimResolverOptions();

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var value = context.User.FindFirst(_options.ClaimType)?.Value;
        return Task.FromResult(string.IsNullOrWhiteSpace(value) ? null : value);
    }
}
