using System.Collections.Concurrent;
using Primitives.RateLimiting.Abstractions;

namespace Primitives.RateLimiting.Internal;

/// <summary>Thread-safe in-memory rate-limit counter store. Not suitable for multi-instance deployments.</summary>
internal sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private sealed record Entry(long Count, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _counters = new(StringComparer.Ordinal);

    public Task<long> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = _counters.AddOrUpdate(
            key,
            _ => new Entry(1, now.Add(window)),
            (_, existing) =>
            {
                if (existing.ExpiresAt <= now)
                    return new Entry(1, now.Add(window));
                return existing with { Count = existing.Count + 1 };
            });
        return Task.FromResult(updated.Count);
    }

    public Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_counters.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            return Task.FromResult(entry.Count);
        return Task.FromResult(0L);
    }

    public Task<TimeSpan> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_counters.TryGetValue(key, out var entry))
        {
            var ttl = entry.ExpiresAt - DateTimeOffset.UtcNow;
            return Task.FromResult(ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero);
        }
        return Task.FromResult(TimeSpan.Zero);
    }

    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        _counters.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
