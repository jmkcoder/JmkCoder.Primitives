namespace Primitives.Billing.Abstractions;

/// <summary>
/// Records feature consumption units for a tenant.
/// </summary>
public interface IUsageMeter
{
    /// <summary>
    /// Records <paramref name="quantity"/> consumed units of <paramref name="feature"/>
    /// for the specified tenant.
    /// </summary>
    Task RecordAsync(
        string tenantId,
        string feature,
        decimal quantity = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total usage of <paramref name="feature"/> for the tenant
    /// within the current billing period.
    /// </summary>
    Task<decimal> GetUsageAsync(
        string tenantId,
        string feature,
        CancellationToken cancellationToken = default);

    /// <summary>Resets the usage counter for <paramref name="feature"/> and tenant (e.g. at billing period rollover).</summary>
    Task ResetAsync(
        string tenantId,
        string feature,
        CancellationToken cancellationToken = default);
}
