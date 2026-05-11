using Primitives.Messaging.Abstractions;
using Primitives.Messaging.Models;
using System.Collections.Concurrent;

namespace Primitives.Messaging.Internal;

/// <summary>
/// Thread-safe in-memory <see cref="IOutboxStore"/> for development and testing.
/// All state is lost when the process restarts.
/// </summary>
public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _store = new();

    /// <inheritdoc/>
    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _store[message.Id] = message;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OutboxMessage> pending = _store.Values
            .Where(m => m.PublishedAt is null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList();

        return Task.FromResult(pending);
    }

    /// <inheritdoc/>
    public Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var msg))
            msg.PublishedAt = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MarkFailedAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var msg))
        {
            msg.Error = reason;
            msg.AttemptCount++;
        }

        return Task.CompletedTask;
    }
}
