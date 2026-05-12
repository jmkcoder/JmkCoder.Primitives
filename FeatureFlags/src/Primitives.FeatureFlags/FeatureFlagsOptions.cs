using Primitives.FeatureFlags.Models;

namespace Primitives.FeatureFlags;

/// <summary>Top-level configuration for the feature flags module.</summary>
public sealed class FeatureFlagsOptions
{
    /// <summary>
    /// Pre-seeded flag definitions applied to the in-memory store on startup.
    /// Ignored when a custom <see cref="Abstractions.IFeatureFlagStore"/> is registered.
    /// </summary>
    public List<FeatureFlag> Flags { get; set; } = [];

    /// <summary>
    /// When <see langword="true"/>, accessing an undefined flag returns <see langword="false"/>
    /// without logging a warning. Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowUndefinedFlags { get; set; } = false;
}
