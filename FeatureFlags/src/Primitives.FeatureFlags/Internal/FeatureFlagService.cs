using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.FeatureFlags.Abstractions;

namespace Primitives.FeatureFlags.Internal;

/// <summary>Default implementation of <see cref="IFeatureFlagService"/>.</summary>
internal sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagStore _store;
    private readonly FeatureFlagsOptions _options;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(
        IFeatureFlagStore store,
        IOptions<FeatureFlagsOptions> options,
        ILogger<FeatureFlagService> logger)
    {
        _store   = store;
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default)
    {
        var flag = await _store.FindAsync(feature, cancellationToken).ConfigureAwait(false);
        return Evaluate(feature, flag, tenantId: null, subjectId: null);
    }

    public async Task<bool> IsEnabledForTenantAsync(string feature, string tenantId, CancellationToken cancellationToken = default)
    {
        var flag = await _store.FindAsync(feature, cancellationToken).ConfigureAwait(false);
        return Evaluate(feature, flag, tenantId, subjectId: null);
    }

    public async Task<bool> IsEnabledForSubjectAsync(string feature, string subjectId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var flag = await _store.FindAsync(feature, cancellationToken).ConfigureAwait(false);
        return Evaluate(feature, flag, tenantId, subjectId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool Evaluate(string feature, Models.FeatureFlag? flag, string? tenantId, string? subjectId)
    {
        if (flag is null)
        {
            if (!_options.AllowUndefinedFlags)
                _logger.LogWarning("Feature flag '{Feature}' is not defined — returning false", feature);
            return false;
        }

        if (flag.ExpiresAt.HasValue && flag.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            _logger.LogDebug("Feature flag '{Feature}' has expired — returning false", feature);
            return false;
        }

        // Per-tenant override takes highest precedence
        if (tenantId is not null && flag.TenantOverrides.TryGetValue(tenantId, out var tenantOverride))
            return tenantOverride;

        // Percentage rollout evaluated deterministically by subject hash
        if (subjectId is not null && flag.RolloutPercentage > 0 && flag.RolloutPercentage < 100)
            return ComputeRolloutBucket(feature, subjectId) < flag.RolloutPercentage;

        if (flag.RolloutPercentage == 100)
            return true;

        return flag.IsEnabled;
    }

    private static int ComputeRolloutBucket(string feature, string subjectId)
    {
        // Deterministic stable hash — same subject always falls in the same bucket
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{feature}:{subjectId}"));
        var value = (uint)(hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24));
        return (int)(value % 100);
    }
}
