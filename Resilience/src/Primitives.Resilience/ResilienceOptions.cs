using Primitives.Resilience.Models;

namespace Primitives.Resilience;

/// <summary>Top-level options for <c>AddPrimitivesResilience()</c>.</summary>
public sealed class ResilienceOptions
{
    /// <summary>
    /// Named pipeline configurations. Each entry registers a <see cref="PipelineOptions"/>
    /// under a string key that is later retrieved via
    /// <see cref="Abstractions.IResiliencePipelineProvider.Get(string)"/>.
    /// </summary>
    public Dictionary<string, PipelineOptions> Pipelines { get; set; } = new();
}
