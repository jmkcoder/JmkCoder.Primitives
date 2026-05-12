using Primitives.FeatureFlags.Models;

namespace Primitives.FeatureFlags.Abstractions;

/// <summary>
/// Persistent store for feature flag definitions and per-tenant overrides.
/// Replace the default in-memory store by registering a custom implementation.
/// </summary>
public interface IFeatureFlagStore
{
    /// <summary>Returns the global flag definition, or <see langword="null"/> if not found.</summary>
    Task<FeatureFlag?> FindAsync(string feature, CancellationToken cancellationToken = default);

    /// <summary>Returns all defined feature flags.</summary>
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a feature flag definition (insert or replace).</summary>
    Task UpsertAsync(FeatureFlag flag, CancellationToken cancellationToken = default);

    /// <summary>Removes a feature flag definition.</summary>
    Task DeleteAsync(string feature, CancellationToken cancellationToken = default);
}
