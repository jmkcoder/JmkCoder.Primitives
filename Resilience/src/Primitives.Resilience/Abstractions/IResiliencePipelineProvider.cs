using Polly;

namespace Primitives.Resilience.Abstractions;

/// <summary>
/// Resolves named resilience pipelines built from <see cref="ResilienceOptions"/>.
/// Inject this instead of Polly's <c>ResiliencePipelineProvider&lt;string&gt;</c>
/// to keep application code free of Polly DI infrastructure dependencies.
/// </summary>
public interface IResiliencePipelineProvider
{
    /// <summary>
    /// Returns the non-generic pipeline registered under <paramref name="pipelineName"/>.
    /// Returns <see cref="ResiliencePipeline.Empty"/> when no pipeline with that name exists.
    /// </summary>
    ResiliencePipeline Get(string pipelineName);

    /// <summary>
    /// Returns the generic pipeline registered under <paramref name="pipelineName"/>.
    /// Returns <see cref="ResiliencePipeline{T}.Empty"/> when no pipeline with that name exists.
    /// </summary>
    ResiliencePipeline<T> Get<T>(string pipelineName);
}
