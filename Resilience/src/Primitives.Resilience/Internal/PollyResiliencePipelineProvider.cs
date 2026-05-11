using Microsoft.Extensions.Options;
using Polly;
using Primitives.Resilience.Abstractions;
using System.Collections.Concurrent;

namespace Primitives.Resilience.Internal;

/// <summary>
/// Builds and caches Polly <see cref="ResiliencePipeline"/> instances from
/// <see cref="ResilienceOptions"/>. Pipelines are constructed lazily on first access
/// and held for the lifetime of the provider (singleton).
/// </summary>
internal sealed class PollyResiliencePipelineProvider : IResiliencePipelineProvider
{
    private readonly ResilienceOptions _options;

    // Separate caches for non-generic and generic pipelines.
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _cache        = new();
    private readonly ConcurrentDictionary<string, object>             _genericCache = new();

    public PollyResiliencePipelineProvider(IOptions<ResilienceOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc/>
    public ResiliencePipeline Get(string pipelineName)
    {
        return _cache.GetOrAdd(pipelineName, name =>
        {
            if (!_options.Pipelines.TryGetValue(name, out var opts))
                return ResiliencePipeline.Empty;

            var builder = new ResiliencePipelineBuilder();
            PipelineConfigurator.Configure(builder, opts);
            return builder.Build();
        });
    }

    /// <inheritdoc/>
    public ResiliencePipeline<T> Get<T>(string pipelineName)
    {
        // Include the type name in the cache key so different T for the same pipeline name
        // are stored independently.
        var key = $"{pipelineName}::{typeof(T).FullName}";

        return (ResiliencePipeline<T>)_genericCache.GetOrAdd(key, _ =>
        {
            if (!_options.Pipelines.TryGetValue(pipelineName, out var opts))
                return (object)ResiliencePipeline<T>.Empty;

            var builder = new ResiliencePipelineBuilder<T>();
            PipelineConfigurator.Configure(builder, opts);
            return (object)builder.Build();
        });
    }
}
