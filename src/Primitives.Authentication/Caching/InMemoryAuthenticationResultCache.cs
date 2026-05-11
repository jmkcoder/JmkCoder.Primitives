using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Caching;

/// <summary>
/// In-process implementation of <see cref="IAuthenticationResultCache"/> backed by
/// <see cref="IMemoryCache"/>. Suitable for single-instance deployments and testing.
/// For multi-instance deployments, implement <see cref="IAuthenticationResultCache"/>
/// backed by a distributed cache (e.g. Redis via IDistributedCache).
/// </summary>
public sealed class InMemoryAuthenticationResultCache : IAuthenticationResultCache
{
    private readonly IMemoryCache _cache;
    private readonly AuthenticationCacheOptions _cacheOptions;
    private readonly TimeProvider _time;

    public InMemoryAuthenticationResultCache(
        IMemoryCache cache,
        IOptions<AuthenticationCacheOptions> cacheOptions,
        TimeProvider time)
    {
        _cache        = cache;
        _cacheOptions = cacheOptions.Value;
        _time         = time;
    }

    /// <inheritdoc/>
    public Task<AuthenticationResult?> GetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(cacheKey, out AuthenticationResult? result);
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task SetAsync(
        string cacheKey,
        AuthenticationResult result,
        CancellationToken cancellationToken = default)
    {
        if (!result.IsSuccess)
            return Task.CompletedTask; // never cache failures

        var effectiveExpiry = result.ExpiresAt.HasValue
            ? result.ExpiresAt.Value - _cacheOptions.EarlyExpiryBuffer
            : _time.GetUtcNow().AddMinutes(5); // fall back to 5-minute default

        if (effectiveExpiry <= _time.GetUtcNow())
            return Task.CompletedTask; // token already (almost) expired; skip caching

        var entry = _cache.CreateEntry(cacheKey);
        entry.Value          = result;
        entry.AbsoluteExpiration = effectiveExpiry;
        entry.Dispose(); // dispose commits the entry

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }
}