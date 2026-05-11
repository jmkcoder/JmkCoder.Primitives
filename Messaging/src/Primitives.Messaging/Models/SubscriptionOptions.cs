namespace Primitives.Messaging.Models;

/// <summary>Options controlling how a message handler consumes from a queue.</summary>
public sealed class SubscriptionOptions
{
    /// <summary>
    /// Maximum number of unacknowledged messages the broker will deliver before waiting.
    /// Default: <c>10</c>.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
}
