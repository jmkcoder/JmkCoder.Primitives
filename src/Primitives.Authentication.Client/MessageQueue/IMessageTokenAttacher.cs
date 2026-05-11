namespace Primitives.Authentication.Client.MessageQueue;

/// <summary>
/// Attaches a Primitives-issued JWT to an outgoing message's headers.
/// Inject this into your message producers/publishers.
/// </summary>
public interface IMessageTokenAttacher
{
    /// <summary>
    /// Acquires a JWT for <paramref name="strategyName"/> and writes it to
    /// <paramref name="headers"/> as <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </summary>
    /// <param name="headers">
    /// Mutable header dictionary of the outgoing message (e.g. AMQP BasicProperties.Headers,
    /// Kafka Headers, Service Bus ApplicationProperties, etc.).
    /// </param>
    /// <param name="strategyName">Name of the registered authentication strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the token was acquired and written; <c>false</c> if authentication failed
    /// (the message may still be sent without a token — caller decides).
    /// </returns>
    Task<bool> AttachAsync(
        IDictionary<string, string> headers,
        string                      strategyName,
        CancellationToken           cancellationToken = default);
}
