using System.Collections.Concurrent;
using Primitives.Billing.Abstractions;
using Primitives.Billing.Models;

namespace Primitives.Billing.Internal;

/// <summary>Thread-safe in-memory entitlement store.</summary>
internal sealed class InMemoryEntitlementStore : IEntitlementStore
{
    private readonly ConcurrentDictionary<string, Entitlement> _entitlements =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<Entitlement?> FindAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
    {
        _entitlements.TryGetValue(Key(tenantId, feature), out var e);
        return Task.FromResult(e);
    }

    public Task<IReadOnlyList<Entitlement>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var all = _entitlements.Values
            .Where(e => string.Equals(e.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<Entitlement>>(all);
    }

    public Task UpsertAsync(Entitlement entitlement, CancellationToken cancellationToken = default)
    {
        _entitlements[Key(entitlement.TenantId, entitlement.Feature)] = entitlement;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
    {
        _entitlements.TryRemove(Key(tenantId, feature), out _);
        return Task.CompletedTask;
    }

    private static string Key(string tenantId, string feature) => $"{tenantId}:{feature}";
}
