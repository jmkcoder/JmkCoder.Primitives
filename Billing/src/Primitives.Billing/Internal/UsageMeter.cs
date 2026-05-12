using Microsoft.Extensions.Logging;
using Primitives.Billing.Abstractions;

namespace Primitives.Billing.Internal;

/// <summary>Default <see cref="IUsageMeter"/> backed by <see cref="IUsageStore"/>.</summary>
internal sealed class UsageMeter : IUsageMeter
{
    private readonly IUsageStore _store;
    private readonly ILogger<UsageMeter> _logger;

    public UsageMeter(IUsageStore store, ILogger<UsageMeter> logger)
    {
        _store  = store;
        _logger = logger;
    }

    public async Task RecordAsync(string tenantId, string feature, decimal quantity = 1, CancellationToken cancellationToken = default)
    {
        await _store.IncrementAsync(tenantId, feature, quantity, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Usage recorded: tenant={TenantId} feature={Feature} qty={Quantity}", tenantId, feature, quantity);
    }

    public Task<decimal> GetUsageAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
        => _store.GetTotalAsync(tenantId, feature, cancellationToken);

    public Task ResetAsync(string tenantId, string feature, CancellationToken cancellationToken = default)
        => _store.ResetAsync(tenantId, feature, cancellationToken);
}
