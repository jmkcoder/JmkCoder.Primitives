using Primitives.Messaging.Models;

namespace Primitives.Messaging.Abstractions;

/// <summary>
/// Processes a single message of type <typeparamref name="T"/> consumed from the broker.
/// </summary>
/// <typeparam name="T">The message payload type.</typeparam>
public interface IMessageHandler<T> where T : notnull
{
    /// <summary>
    /// Handles the message.
    /// </summary>
    /// <param name="context">The message context, including payload and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="ConsumeResult.Ack"/> to acknowledge and remove from the queue,
    /// <see cref="ConsumeResult.Nack"/> to dead-letter,
    /// or <see cref="ConsumeResult.Requeue"/> to return to the queue for retry.
    /// </returns>
    Task<ConsumeResult> HandleAsync(MessageContext<T> context, CancellationToken cancellationToken);
}
