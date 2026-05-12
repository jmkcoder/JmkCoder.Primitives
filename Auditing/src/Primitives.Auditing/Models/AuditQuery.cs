namespace Primitives.Auditing.Models;

/// <summary>Parameters for querying the audit trail.</summary>
public sealed class AuditQuery
{
    /// <summary>Filter by tenant. <see langword="null"/> returns events for all tenants.</summary>
    public string? TenantId { get; init; }

    /// <summary>Filter by actor identifier.</summary>
    public string? ActorId { get; init; }

    /// <summary>Filter by action string (exact match, case-insensitive).</summary>
    public string? Action { get; init; }

    /// <summary>Filter by resource type.</summary>
    public string? ResourceType { get; init; }

    /// <summary>Filter by resource identifier.</summary>
    public string? ResourceId { get; init; }

    /// <summary>Return only events at or after this timestamp (inclusive).</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Return only events before this timestamp (exclusive).</summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>Filter by outcome.</summary>
    public AuditOutcome? Outcome { get; init; }

    /// <summary>Maximum number of results to return. Defaults to 50.</summary>
    public int PageSize { get; init; } = 50;

    /// <summary>Zero-based page offset.</summary>
    public int Page { get; init; } = 0;
}
