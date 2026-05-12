using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Primitives.FeatureFlags.Abstractions;

namespace Primitives.FeatureFlags.Internal;

/// <summary>Seeds flags from <see cref="FeatureFlagsOptions.Flags"/> into the store at application start.</summary>
internal sealed class FeatureFlagSeedService : IHostedService
{
    private readonly IFeatureFlagStore _store;
    private readonly FeatureFlagsOptions _options;

    public FeatureFlagSeedService(IFeatureFlagStore store, IOptions<FeatureFlagsOptions> options)
    {
        _store   = store;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var flag in _options.Flags)
            await _store.UpsertAsync(flag, cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
