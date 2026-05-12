using System.Collections.Concurrent;
using Primitives.Billing.Abstractions;
using Primitives.Billing.Models;

namespace Primitives.Billing.Internal;

/// <summary>Thread-safe in-memory usage store.</summary>
internal sealed class InMemoryUsageStore : IUsageStore
{
    // Key: tenantId:feature → total
    private readonly ConcurrentDictionary<string, decimal> _totals =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastUpdated =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public Task IncrementAsync(string tenantId, string feature, decimal quantity, CancellationToken cancellationToken = default)
    {
        var k = Key(tenantId, feature);
        lock (_lock)
        {
            _totals.AddOrUpdate(k, quantity, (_, existing) => existing + quantity);
            _lastUpdated[k] = DateTimeOffset.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<decimal> GetTotalAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
    {
        _totals.TryGetValue(Key(tenantId, feature), out var total);
        return Task.FromResult(total);
    }

    public Task ResetAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
    {
        var k = Key(tenantId, feature);
        _totals.TryRemove(k, out _);
        _lastUpdated.TryRemove(k, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UsageRecord>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var prefix = $"{tenantId}:";
        var records = _totals
            .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new UsageRecord
            {
                TenantId      = tenantId,
                Feature       = kvp.Key[prefix.Length..],
                TotalUnits    = kvp.Value,
                LastUpdatedAt = _lastUpdated.TryGetValue(kvp.Key, out var ts) ? ts : DateTimeOffset.UtcNow,
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<UsageRecord>>(records);
    }

    private static string Key(string tenantId, string feature) => $"{tenantId}:{feature}";
}
