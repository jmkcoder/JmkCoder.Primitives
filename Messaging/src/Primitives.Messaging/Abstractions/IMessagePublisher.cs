using Primitives.Messaging.Models;

namespace Primitives.Messaging.Abstractions;

/// <summary>
/// Publishes messages to the configured broker (or in-process bus for development/testing).
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes <paramref name="message"/> to the broker.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Per-message options that override global defaults.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(
        T message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : notnull;
}
