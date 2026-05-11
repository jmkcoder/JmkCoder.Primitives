using Microsoft.Extensions.Options;
using Primitives.Multitenancy.Abstractions;
using Primitives.Multitenancy.Models;

namespace Primitives.Multitenancy.Internal;

/// <summary>
/// In-memory tenant store backed by the <see cref="MultitenancyOptions.Tenants"/> list.
/// Suitable for development and scenarios where tenants are known at startup.
/// Replace with a custom <see cref="ITenantStore"/> for database-backed tenant resolution.
/// </summary>
internal sealed class InMemoryTenantStore : ITenantStore
{
    private readonly IReadOnlyDictionary<string, Tenant> _tenants;

    public InMemoryTenantStore(IOptions<MultitenancyOptions> options)
    {
        _tenants = options.Value.Tenants
            .ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
    }

    public Task<Tenant?> FindByIdentifierAsync(string identifier, CancellationToken ct = default)
    {
        _tenants.TryGetValue(identifier, out var tenant);
        return Task.FromResult(tenant);
    }
}
