namespace Primitives.Auditing.Models;

/// <summary>Immutable record of a single auditable action.</summary>
public sealed class AuditEvent
{
    /// <summary>Unique identifier for this audit record.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp of the event.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Identifier of the user, service, or system that performed the action.</summary>
    public required string ActorId { get; init; }

    /// <summary>Optional display name of the actor (for human-readable logs).</summary>
    public string? ActorName { get; init; }

    /// <summary>Action performed (e.g. <c>"invoice.created"</c>, <c>"user.deleted"</c>).</summary>
    public required string Action { get; init; }

    /// <summary>Type of resource targeted (e.g. <c>"invoice"</c>, <c>"user"</c>).</summary>
    public string? ResourceType { get; init; }

    /// <summary>Identifier of the targeted resource.</summary>
    public string? ResourceId { get; init; }

    /// <summary>Tenant that owns the event. <see langword="null"/> for system-level events.</summary>
    public string? TenantId { get; init; }

    /// <summary>Outcome of the action.</summary>
    public AuditOutcome Outcome { get; init; } = AuditOutcome.Success;

    /// <summary>IP address of the originating request, if available.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Arbitrary additional context serialized as key–value pairs.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
