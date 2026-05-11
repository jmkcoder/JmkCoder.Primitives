namespace Primitives.Messaging.Models;

/// <summary>Per-message publish options that override the global <see cref="MessagingOptions"/>.</summary>
public sealed class PublishOptions
{
    /// <summary>Override the exchange the message is published to.</summary>
    public string? Exchange { get; set; }

    /// <summary>
    /// Override the routing key.
    /// Defaults to the message type name in lowercase when not specified.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>Correlation identifier for distributed tracing.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Optional per-message TTL. The message is discarded after this duration.</summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>Additional application-level headers to attach to the message.</summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
