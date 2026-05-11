namespace Primitives.Messaging.Models;

/// <summary>A message stored in the transactional outbox, pending delivery to the broker.</summary>
public sealed class OutboxMessage
{
    /// <summary>Unique identifier for this outbox entry.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Fully-qualified CLR type name of the message payload.</summary>
    public required string MessageType { get; set; }

    /// <summary>JSON-serialized message payload.</summary>
    public required string Payload { get; set; }

    /// <summary>Target exchange (or topic) to publish to.</summary>
    public required string Exchange { get; set; }

    /// <summary>Routing key to use when publishing.</summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this entry was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when the message was successfully published,
    /// or <c>null</c> if still pending.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Error message from the last failed publish attempt, or <c>null</c>.</summary>
    public string? Error { get; set; }

    /// <summary>Total number of publish attempts made.</summary>
    public int AttemptCount { get; set; }
}
