using Primitives.Billing.Models;

namespace Primitives.Billing.Abstractions;

/// <summary>
/// Persistent store for plan definitions and tenant entitlements.
/// Replace the default in-memory implementation for production.
/// </summary>
public interface IEntitlementStore
{
    /// <summary>Returns the entitlement for <paramref name="tenantId"/> and <paramref name="feature"/>, or <see langword="null"/>.</summary>
    Task<Entitlement?> FindAsync(string tenantId, string feature, CancellationToken cancellationToken = default);

    /// <summary>Returns all entitlements for a tenant.</summary>
    Task<IReadOnlyList<Entitlement>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Persists an entitlement definition (insert or replace).</summary>
    Task UpsertAsync(Entitlement entitlement, CancellationToken cancellationToken = default);

    /// <summary>Removes an entitlement for a tenant and feature.</summary>
    Task DeleteAsync(string tenantId, string feature, CancellationToken cancellationToken = default);
}
