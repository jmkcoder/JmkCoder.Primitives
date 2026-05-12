using System.Text.Json;

namespace Primitives.Notifications.Models;

/// <summary>An outbound webhook event dispatched to tenant-registered endpoints.</summary>
public sealed class WebhookEvent
{
    /// <summary>Unique event identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Event type (e.g. <c>"invoice.paid"</c>, <c>"user.created"</c>).</summary>
    public required string EventType { get; init; }

    /// <summary>Tenant that owns this event.</summary>
    public required string TenantId { get; init; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Strongly-typed payload serialized as JSON when dispatching.</summary>
    public required object Payload { get; init; }
}
