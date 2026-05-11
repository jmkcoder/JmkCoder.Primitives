using Primitives.Messaging.Models;

namespace Primitives.Messaging.Internal;

/// <summary>
/// Describes a single message handler registration — its queue/exchange binding and the
/// type-erased dispatch delegate. Registered as a singleton in DI and consumed by
/// broker-specific consumer services (e.g. <c>RabbitMQConsumerService</c>).
/// </summary>
public sealed class MessageHandlerRegistration
{
    /// <summary>Queue (or subscription topic) name to consume from.</summary>
    public required string QueueName { get; init; }

    /// <summary>Exchange to bind the queue to. Use <c>string.Empty</c> for the default exchange.</summary>
    public string Exchange { get; init; } = string.Empty;

    /// <summary>Routing key pattern for the queue binding. Default: <c>#</c> (match-all).</summary>
    public string RoutingKey { get; init; } = "#";

    /// <summary>Subscription-level options (prefetch count, etc.).</summary>
    public required SubscriptionOptions Options { get; init; }

    /// <summary>
    /// Type-erased dispatch delegate. Deserializes <paramref name="body"/>, builds
    /// a typed <see cref="MessageContext{T}"/>, and invokes all registered handlers.
    /// </summary>
    public required Func<IServiceProvider, ReadOnlyMemory<byte>, MessageMetadata, CancellationToken, Task<ConsumeResult>> Dispatch { get; init; }
}
