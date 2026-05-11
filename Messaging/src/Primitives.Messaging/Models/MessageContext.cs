namespace Primitives.Messaging.Models;

/// <summary>Contains the deserialized message and its associated delivery metadata.</summary>
/// <typeparam name="T">The message payload type.</typeparam>
public sealed class MessageContext<T> where T : notnull
{
    /// <summary>The deserialized message payload.</summary>
    public required T Message { get; init; }

    /// <summary>Metadata associated with this message delivery.</summary>
    public required MessageMetadata Metadata { get; init; }
}
