namespace Primitives.Notifications.Models;

/// <summary>A tenant-registered endpoint that receives webhook events.</summary>
public sealed class WebhookEndpoint
{
    /// <summary>Unique endpoint identifier.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Tenant that owns this endpoint.</summary>
    public required string TenantId { get; init; }

    /// <summary>HTTPS URL to POST webhook events to.</summary>
    public required string Url { get; init; }

    /// <summary>
    /// Event types this endpoint subscribes to (e.g. <c>["invoice.paid", "user.created"]</c>).
    /// Use <c>["*"]</c> to subscribe to all events.
    /// </summary>
    public IReadOnlyList<string> EventTypes { get; init; } = [];

    /// <summary>Secret used to compute the HMAC-SHA256 signature sent in the <c>X-Webhook-Signature</c> header.</summary>
    public required string Secret { get; init; }

    /// <summary>Whether this endpoint is active.</summary>
    public bool IsActive { get; init; } = true;
}
