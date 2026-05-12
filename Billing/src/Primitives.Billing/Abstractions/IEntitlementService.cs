using Primitives.Billing.Models;

namespace Primitives.Billing.Abstractions;

/// <summary>
/// Checks whether a tenant is allowed to consume a feature given their plan and current usage.
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Returns <see langword="true"/> when the tenant is within their quota for <paramref name="feature"/>.
    /// Returns <see langword="true"/> for unlimited entitlements.
    /// </summary>
    Task<bool> IsAllowedAsync(
        string tenantId,
        string feature,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the entitlement definition for <paramref name="tenantId"/> and <paramref name="feature"/>, or <see langword="null"/>.</summary>
    Task<Entitlement?> GetEntitlementAsync(
        string tenantId,
        string feature,
        CancellationToken cancellationToken = default);
}
