namespace Primitives.Authentication.AspNetCore.MessageQueue;

/// <summary>
/// Abstracts the transport-specific way of extracting a Bearer JWT from an
/// incoming message.  Implement this for each message-queue transport you use
/// (RabbitMQ, Kafka, Azure Service Bus, NATS, etc.).
/// </summary>
/// <example>
/// RabbitMQ example:
/// <code>
/// public sealed class RabbitMessageAuthContext : IMessageAuthenticationContext
/// {
///     private readonly IBasicProperties _props;
///     public RabbitMessageAuthContext(IBasicProperties props) => _props = props;
///
///     public string? GetToken() =>
///         _props.Headers?.TryGetValue("Authorization", out var v) == true
///             ? Encoding.UTF8.GetString((byte[])v!).Replace("Bearer ", "")
///             : null;
/// }
/// </code>
/// </example>
public interface IMessageAuthenticationContext
{
    /// <summary>
    /// Returns the raw JWT (without the "Bearer " prefix), or <c>null</c>
    /// if no token is present in the message.
    /// </summary>
    string? GetToken();
}
