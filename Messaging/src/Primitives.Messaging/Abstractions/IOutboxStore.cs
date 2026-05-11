using Primitives.Messaging.Models;

namespace Primitives.Messaging.Abstractions;

/// <summary>
/// Persistent store for the transactional outbox pattern.
/// Save messages within the same database transaction as your domain changes,
/// then let a background relay publish them to the broker asynchronously.
/// </summary>
public interface IOutboxStore
{
    /// <summary>Persists an outbox message — typically within a database transaction.</summary>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="batchSize"/> messages that have not yet been published.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marks a message as successfully published.</summary>
    Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Records a failed publish attempt and increments the attempt counter.</summary>
    Task MarkFailedAsync(Guid id, string reason, CancellationToken cancellationToken = default);
}
