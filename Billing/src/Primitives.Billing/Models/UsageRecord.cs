namespace Primitives.Billing.Models;

/// <summary>A single usage accumulation record for a tenant and feature.</summary>
public sealed class UsageRecord
{
    /// <summary>Tenant that consumed the units.</summary>
    public required string TenantId { get; init; }

    /// <summary>Feature being metered.</summary>
    public required string Feature { get; init; }

    /// <summary>Total units consumed since the last reset.</summary>
    public required decimal TotalUnits { get; init; }

    /// <summary>UTC timestamp of the last increment.</summary>
    public DateTimeOffset LastUpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
