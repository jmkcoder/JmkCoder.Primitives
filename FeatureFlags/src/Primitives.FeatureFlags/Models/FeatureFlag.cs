namespace Primitives.FeatureFlags.Models;

/// <summary>Defines a feature flag and its evaluation rules.</summary>
public sealed class FeatureFlag
{
    /// <summary>Unique name of the feature flag (e.g. <c>"new-dashboard"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Global enabled state used when no tenant or subject override applies.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Percentage (0–100) of subjects for whom the flag is enabled via rollout.
    /// <c>0</c> disables rollout; <c>100</c> is equivalent to <see cref="IsEnabled"/>=<see langword="true"/>.
    /// </summary>
    public int RolloutPercentage { get; init; }

    /// <summary>Per-tenant overrides. Key is tenant ID; value overrides <see cref="IsEnabled"/>.</summary>
    public IReadOnlyDictionary<string, bool> TenantOverrides { get; init; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional UTC timestamp after which this flag is automatically disabled.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
