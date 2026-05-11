using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Primitives.Observability.Abstractions;

namespace Primitives.Observability.Internal;

internal sealed class ActivitySourceProvider : IActivitySourceProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, ActivitySource> _sources =
        new(StringComparer.Ordinal);

    private readonly string _version;

    public ActivitySourceProvider(IOptions<ObservabilityOptions> options)
        => _version = options.Value.ServiceVersion;

    public ActivitySource GetSource(string name)
        => _sources.GetOrAdd(name, static (n, v) => new ActivitySource(n, v), _version);

    public void Dispose()
    {
        foreach (var source in _sources.Values)
            source.Dispose();
        _sources.Clear();
    }
}
