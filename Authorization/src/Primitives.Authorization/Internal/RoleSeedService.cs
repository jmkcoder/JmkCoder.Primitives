using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Primitives.Authorization.Abstractions;

namespace Primitives.Authorization.Internal;

/// <summary>Seeds roles from <see cref="AuthorizationOptions.Roles"/> into the store at startup.</summary>
internal sealed class RoleSeedService : IHostedService
{
    private readonly IRoleStore _store;
    private readonly AuthorizationOptions _options;

    public RoleSeedService(IRoleStore store, IOptions<AuthorizationOptions> options)
    {
        _store   = store;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var role in _options.Roles)
            await _store.UpsertAsync(role, cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
