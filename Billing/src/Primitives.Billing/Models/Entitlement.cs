namespace Primitives.Billing.Models;

/// <summary>Defines what a tenant is allowed to consume for a specific feature.</summary>
public sealed class Entitlement
{
    /// <summary>Tenant this entitlement belongs to.</summary>
    public required string TenantId { get; init; }

    /// <summary>Feature or resource being metered (e.g. <c>"api-calls"</c>, <c>"seats"</c>).</summary>
    public required string Feature { get; init; }

    /// <summary>
    /// Maximum units allowed per billing period. <see langword="null"/> means unlimited.
    /// </summary>
    public decimal? Limit { get; init; }

    /// <summary>Optional human-readable name of the plan tier (e.g. <c>"Pro"</c>, <c>"Enterprise"</c>).</summary>
    public string? PlanName { get; init; }

    /// <summary>UTC start of the current billing period.</summary>
    public DateTimeOffset? PeriodStart { get; init; }

    /// <summary>UTC end of the current billing period.</summary>
    public DateTimeOffset? PeriodEnd { get; init; }
}
