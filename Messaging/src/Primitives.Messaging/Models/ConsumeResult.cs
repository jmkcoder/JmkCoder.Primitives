namespace Primitives.Messaging.Models;

/// <summary>Indicates how a message handler has processed a received message.</summary>
public enum ConsumeResult
{
    /// <summary>Message processed successfully. Acknowledge and remove from the queue.</summary>
    Ack,

    /// <summary>Processing failed permanently. Dead-letter the message.</summary>
    Nack,

    /// <summary>Processing failed transiently. Requeue the message for retry.</summary>
    Requeue,
}
