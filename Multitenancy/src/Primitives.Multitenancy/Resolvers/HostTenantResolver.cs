using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Resolvers;

/// <summary>Options for <see cref="HostTenantResolver"/>.</summary>
public sealed class HostResolverOptions
{
    /// <summary>
    /// Zero-based index of the subdomain segment to use as the tenant identifier.
    /// For <c>acme.example.com</c>, index <c>0</c> yields <c>"acme"</c>.
    /// Ignored when <see cref="HostMap"/> has a matching entry.
    /// Defaults to <c>0</c>.
    /// </summary>
    public int SubdomainIndex { get; set; } = 0;

    /// <summary>
    /// Explicit hostname-to-tenant-identifier mapping.
    /// Takes precedence over <see cref="SubdomainIndex"/>.
    /// Keys are matched case-insensitively.
    /// </summary>
    public Dictionary<string, string> HostMap { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Resolves the tenant from the HTTP <c>Host</c> header.
/// Supports subdomain extraction and an explicit hostname-to-tenant map.
/// </summary>
public sealed class HostTenantResolver : ITenantResolverStrategy
{
    private readonly HostResolverOptions _options;

    public HostTenantResolver(HostResolverOptions? options = null)
        => _options = options ?? new HostResolverOptions();

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
            return Task.FromResult<string?>(null);

        // Explicit map takes precedence
        if (_options.HostMap.TryGetValue(host, out var mapped))
            return Task.FromResult<string?>(mapped);

        // Subdomain extraction
        var parts = host.Split('.');
        if (parts.Length <= 1)
            return Task.FromResult<string?>(null); // no subdomain (e.g. "localhost")

        var index = _options.SubdomainIndex;
        if (index < 0 || index >= parts.Length)
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(parts[index]);
    }
}
