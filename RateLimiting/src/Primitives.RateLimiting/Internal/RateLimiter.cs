using Microsoft.Extensions.Options;
using Primitives.RateLimiting.Abstractions;
using Primitives.RateLimiting.Models;

namespace Primitives.RateLimiting.Internal;

/// <summary>
/// Default <see cref="IRateLimiter"/> implementation backed by <see cref="IRateLimitStore"/>.
/// Supports fixed-window and (approximated) sliding-window algorithms.
/// </summary>
internal sealed class RateLimiter : IRateLimiter
{
    private readonly IRateLimitStore _store;
    private readonly RateLimitingOptions _options;

    public RateLimiter(IRateLimitStore store, IOptions<RateLimitingOptions> options)
    {
        _store   = store;
        _options = options.Value;
    }

    public async Task<RateLimitResult> AcquireAsync(string policy, string key, CancellationToken cancellationToken = default)
    {
        var pol = FindPolicy(policy);
        var storeKey = $"{policy}:{key}";
        var count = await _store.IncrementAsync(storeKey, pol.Window, cancellationToken).ConfigureAwait(false);
        var ttl   = await _store.GetTtlAsync(storeKey, cancellationToken).ConfigureAwait(false);
        return BuildResult(count, pol.PermitLimit, ttl);
    }

    public async Task<RateLimitResult> PeekAsync(string policy, string key, CancellationToken cancellationToken = default)
    {
        var pol = FindPolicy(policy);
        var storeKey = $"{policy}:{key}";
        var count = await _store.GetCountAsync(storeKey, cancellationToken).ConfigureAwait(false);
        var ttl   = await _store.GetTtlAsync(storeKey, cancellationToken).ConfigureAwait(false);
        return BuildResult(count, pol.PermitLimit, ttl);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private RateLimitPolicy FindPolicy(string name)
    {
        var pol = _options.Policies.Find(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Rate-limit policy '{name}' is not configured.");
        return pol;
    }

    private static RateLimitResult BuildResult(long count, long limit, TimeSpan retryAfter) =>
        new()
        {
            IsAllowed  = count <= limit,
            Count      = count,
            Limit      = limit,
            RetryAfter = retryAfter,
        };
}
