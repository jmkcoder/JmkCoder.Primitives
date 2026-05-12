namespace Primitives.FeatureFlags.Abstractions;

/// <summary>
/// Evaluates whether a named feature flag is enabled, optionally scoped to a tenant or subject.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="feature"/> is enabled globally.
    /// </summary>
    Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="feature"/> is enabled for the specified tenant.
    /// Per-tenant overrides take precedence over the global default.
    /// </summary>
    Task<bool> IsEnabledForTenantAsync(string feature, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="feature"/> is enabled for the specified subject
    /// (e.g. a user ID), evaluating percentage rollout and per-subject overrides.
    /// </summary>
    Task<bool> IsEnabledForSubjectAsync(string feature, string subjectId, string? tenantId = null, CancellationToken cancellationToken = default);
}
