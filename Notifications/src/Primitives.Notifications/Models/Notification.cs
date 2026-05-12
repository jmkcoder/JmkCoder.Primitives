namespace Primitives.Notifications.Models;

/// <summary>A transactional notification to be delivered via registered channels.</summary>
public sealed class Notification
{
    /// <summary>Recipient identifier (user ID, email address, phone number, etc.).</summary>
    public required string Recipient { get; init; }

    /// <summary>Notification subject or title.</summary>
    public required string Subject { get; init; }

    /// <summary>Plain-text body.</summary>
    public required string Body { get; init; }

    /// <summary>Optional HTML body; used by channels that support rich content.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// Channel names to use (e.g. <c>["email"]</c>).
    /// Empty list means all registered channels that can handle the notification.
    /// </summary>
    public IReadOnlyList<string> Channels { get; init; } = [];

    /// <summary>Tenant identifier, if applicable.</summary>
    public string? TenantId { get; init; }

    /// <summary>Arbitrary metadata passed to channel implementations.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
