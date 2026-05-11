namespace Primitives.Messaging.Models;

/// <summary>Broker-provided and framework metadata attached to a received message.</summary>
public sealed class MessageMetadata
{
    /// <summary>Unique identifier assigned by the publisher.</summary>
    public required string MessageId { get; init; }

    /// <summary>Correlation identifier for distributed tracing, or <c>null</c> if not set.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>UTC timestamp when the message was published.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Number of times this message has been delivered (1 = first delivery).</summary>
    public int DeliveryCount { get; init; } = 1;

    /// <summary>Application-level headers carried with the message.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
