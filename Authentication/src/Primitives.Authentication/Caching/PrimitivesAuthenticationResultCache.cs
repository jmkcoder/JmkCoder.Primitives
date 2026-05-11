using Microsoft.Extensions.Options;
using Primitives.Caching.Abstractions;
using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Caching;

/// <summary>
/// <see cref="IAuthenticationResultCache"/> backed by <see cref="ICacheService"/>
/// from <c>Primitives.Caching</c>. Works with any registered backend — in-memory,
/// distributed, or Redis — without code changes.
/// </summary>
/// <remarks>
/// Keys are prefixed with <c>prim:auth:</c> to avoid collisions with other
/// parts of the application that share the same <see cref="ICacheService"/> instance.
/// </remarks>
internal sealed class PrimitivesAuthenticationResultCache : IAuthenticationResultCache
{
    private const string KeyPrefix = "prim:auth:";

    private readonly ICacheService _cache;
    private readonly AuthenticationCacheOptions _opts;
    private readonly TimeProvider _time;

    public PrimitivesAuthenticationResultCache(
        ICacheService cache,
        IOptions<AuthenticationCacheOptions> opts,
        TimeProvider time)
    {
        _cache = cache;
        _opts  = opts.Value;
        _time  = time;
    }

    /// <inheritdoc/>
    public Task<AuthenticationResult?> GetAsync(
        string key,
        CancellationToken cancellationToken = default) =>
        _cache.GetAsync<AuthenticationResult>(KeyPrefix + key, cancellationToken);

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        AuthenticationResult result,
        CancellationToken cancellationToken = default)
    {
        if (!result.IsSuccess)
            return;

        var now             = _time.GetUtcNow();
        var effectiveExpiry = result.ExpiresAt.HasValue
            ? result.ExpiresAt.Value - now - _opts.EarlyExpiryBuffer
            : TimeSpan.FromMinutes(5);

        if (effectiveExpiry <= TimeSpan.Zero)
            return; // token already (nearly) expired — nothing worth caching

        await _cache.SetAsync(
            key:     KeyPrefix + key,
            value:   result,
            options: new CacheEntryOptions { AbsoluteExpiration = effectiveExpiry },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _cache.InvalidateAsync(KeyPrefix + key, cancellationToken);
}
