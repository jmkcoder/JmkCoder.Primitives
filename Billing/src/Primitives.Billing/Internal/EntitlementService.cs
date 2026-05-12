using Microsoft.Extensions.Logging;
using Primitives.Billing.Abstractions;
using Primitives.Billing.Models;

namespace Primitives.Billing.Internal;

/// <summary>Default <see cref="IEntitlementService"/> that compares current usage against plan limits.</summary>
internal sealed class EntitlementService : IEntitlementService
{
    private readonly IEntitlementStore _entitlementStore;
    private readonly IUsageStore _usageStore;
    private readonly ILogger<EntitlementService> _logger;

    public EntitlementService(
        IEntitlementStore entitlementStore,
        IUsageStore usageStore,
        ILogger<EntitlementService> logger)
    {
        _entitlementStore = entitlementStore;
        _usageStore       = usageStore;
        _logger           = logger;
    }

    public async Task<bool> IsAllowedAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
    {
        var entitlement = await _entitlementStore.FindAsync(tenantId, feature, cancellationToken).ConfigureAwait(false);

        if (entitlement is null)
        {
            _logger.LogDebug("No entitlement found for tenant={TenantId} feature={Feature} — allowing by default", tenantId, feature);
            return true;
        }

        if (entitlement.Limit is null)
            return true; // Unlimited

        var usage = await _usageStore.GetTotalAsync(tenantId, feature, cancellationToken).ConfigureAwait(false);
        var allowed = usage < entitlement.Limit.Value;

        if (!allowed)
            _logger.LogWarning("Entitlement exceeded: tenant={TenantId} feature={Feature} usage={Usage} limit={Limit}",
                tenantId, feature, usage, entitlement.Limit.Value);

        return allowed;
    }

    public Task<Entitlement?> GetEntitlementAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
        => _entitlementStore.FindAsync(tenantId, feature, cancellationToken);
}
