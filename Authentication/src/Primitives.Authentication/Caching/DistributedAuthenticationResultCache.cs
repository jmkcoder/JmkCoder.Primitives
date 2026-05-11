using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;
using System.Text.Json;

namespace Primitives.Authentication.Caching;

/// <summary>
/// Distributed implementation of <see cref="IAuthenticationResultCache"/> backed by
/// <see cref="IDistributedCache"/>. Suitable for multi-instance / horizontally-scaled deployments.
/// </summary>
/// <remarks>
/// Register a concrete distributed cache before calling
/// <c>builder.AddDistributedResultCache()</c>:
/// <code>
/// services.AddStackExchangeRedisCache(o => o.Configuration = "localhost");
/// services.AddAuthentication().AddDistributedResultCache();
/// </code>
///
/// Cache keys are prefixed with <c>prim:auth:</c> to avoid collisions.
/// </remarks>
public sealed class DistributedAuthenticationResultCache : IAuthenticationResultCache
{
    private const string KeyPrefix = "prim:auth:";

    private readonly IDistributedCache           _cache;
    private readonly AuthenticationCacheOptions  _opts;
    private readonly TimeProvider                _time;

    public DistributedAuthenticationResultCache(
        IDistributedCache                    cache,
        IOptions<AuthenticationCacheOptions> opts,
        TimeProvider                         time)
    {
        _cache = cache;
        _opts  = opts.Value;
        _time  = time;
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult?> GetAsync(
        string            cacheKey,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(KeyPrefix + cacheKey, cancellationToken)
                                .ConfigureAwait(false);
        if (bytes is null) return null;

        var dto = JsonSerializer.Deserialize<CachedResultDto>(bytes);
        return dto?.ToResult();
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string              cacheKey,
        AuthenticationResult result,
        CancellationToken   cancellationToken = default)
    {
        if (!result.IsSuccess) return;

        var now             = _time.GetUtcNow();
        var effectiveExpiry = result.ExpiresAt.HasValue
            ? result.ExpiresAt.Value - _opts.EarlyExpiryBuffer
            : now.AddMinutes(5);

        if (effectiveExpiry <= now) return;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(CachedResultDto.From(result));

        await _cache.SetAsync(KeyPrefix + cacheKey, bytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = effectiveExpiry,
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(KeyPrefix + cacheKey, cancellationToken);

    // ── Serialization DTO ────────────────────────────────────────────────────
    // Exception is not safely serializable across processes — omitted here.

    private sealed record CachedResultDto(
        bool                        IsSuccess,
        string?                     AccessToken,
        string?                     TokenType,
        DateTimeOffset?             ExpiresAt,
        Dictionary<string, string>? Claims,
        string?                     Subject,
        string?                     RefreshToken,
        string?                     ErrorMessage)
    {
        public static CachedResultDto From(AuthenticationResult r) => new(
            r.IsSuccess, r.AccessToken, r.TokenType, r.ExpiresAt,
            r.Claims?.ToDictionary(k => k.Key, v => v.Value),
            r.Subject, r.RefreshToken, r.ErrorMessage);

        public AuthenticationResult ToResult() => IsSuccess
            ? AuthenticationResult.Success(AccessToken!, TokenType ?? "Bearer",
                ExpiresAt, Claims, Subject, RefreshToken)
            : AuthenticationResult.Failure(ErrorMessage ?? "Unknown error");
    }
}
