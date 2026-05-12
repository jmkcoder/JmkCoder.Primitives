using System.Collections.Concurrent;
using Primitives.FeatureFlags.Abstractions;
using Primitives.FeatureFlags.Models;

namespace Primitives.FeatureFlags.Internal;

/// <summary>Thread-safe in-memory feature flag store suitable for development and testing.</summary>
internal sealed class InMemoryFeatureFlagStore : IFeatureFlagStore
{
    private readonly ConcurrentDictionary<string, FeatureFlag> _flags =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<FeatureFlag?> FindAsync(string feature, CancellationToken cancellationToken = default)
    {
        _flags.TryGetValue(feature, out var flag);
        return Task.FromResult(flag);
    }

    public Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<FeatureFlag>>(_flags.Values.ToList());

    public Task UpsertAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        _flags[flag.Name] = flag;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string feature, CancellationToken cancellationToken = default)
    {
        _flags.TryRemove(feature, out _);
        return Task.CompletedTask;
    }
}
