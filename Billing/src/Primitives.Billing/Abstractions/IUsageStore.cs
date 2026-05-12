using Primitives.Billing.Models;

namespace Primitives.Billing.Abstractions;

/// <summary>
/// Persistent store for usage records.
/// Replace the default in-memory implementation for production.
/// </summary>
public interface IUsageStore
{
    /// <summary>Adds <paramref name="quantity"/> to the usage counter for the tenant and feature.</summary>
    Task IncrementAsync(string tenantId, string feature, decimal quantity, CancellationToken cancellationToken = default);

    /// <summary>Returns the total usage accumulated since the last reset.</summary>
    Task<decimal> GetTotalAsync(string tenantId, string feature, CancellationToken cancellationToken = default);

    /// <summary>Resets the usage counter to zero.</summary>
    Task ResetAsync(string tenantId, string feature, CancellationToken cancellationToken = default);

    /// <summary>Returns all usage records for a tenant.</summary>
    Task<IReadOnlyList<UsageRecord>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default);
}
