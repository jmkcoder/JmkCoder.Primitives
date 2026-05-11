using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.Json;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Distributed implementation of <see cref="IRefreshTokenStore"/> backed by
/// <see cref="IDistributedCache"/>. Suitable for multi-instance / horizontally-scaled
/// deployments (Redis, SQL, etc.).
/// </summary>
/// <remarks>
/// Register a concrete distributed cache before calling
/// <c>builder.AddDistributedRefreshTokenStore()</c>:
/// <code>
/// services.AddStackExchangeRedisCache(o => o.Configuration = "localhost");
/// services.AddAuthentication().AddJwtTokenIssuance(...).AddDistributedRefreshTokenStore();
/// </code>
///
/// Cache keys are prefixed with <c>prim:rt:</c> to avoid collisions.
///
/// <strong>Limitation:</strong> Successor-chain revocation (revoking all tokens issued after a
/// compromised token) is not supported in this implementation. Each token is revoked individually.
/// For full reuse-detection chain revocation, use a transactional database-backed implementation.
/// </remarks>
public sealed class DistributedRefreshTokenStore : IRefreshTokenStore
{
    private const string KeyPrefix = "prim:rt:";

    private readonly IDistributedCache _cache;
    private readonly JwtOptions        _options;
    private readonly TimeProvider      _time;

    public DistributedRefreshTokenStore(
        IDistributedCache    cache,
        IOptions<JwtOptions> options,
        TimeProvider         time)
    {
        _cache   = cache;
        _options = options.Value;
        _time    = time;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string            subject,
        CancellationToken cancellationToken = default)
    {
        var now   = _time.GetUtcNow();
        var token = CreateSecureToken();

        var entry = new RefreshTokenEntry
        {
            Token     = token,
            Subject   = subject,
            CreatedAt = now,
            ExpiresAt = now.Add(_options.RefreshTokenLifetime),
        };

        await StoreAsync(token, entry, cancellationToken).ConfigureAwait(false);
        return token;
    }

    /// <inheritdoc/>
    public async Task<RefreshTokenRotationResult> ValidateAndRotateAsync(
        string            token,
        CancellationToken cancellationToken = default)
    {
        var entry = await LoadAsync(token, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return RefreshTokenRotationResult.Failure("Refresh token not found.");

        var now = _time.GetUtcNow();
        if (!entry.IsActiveAt(now))
        {
            var reason = entry.IsRevoked
                ? "Refresh token has been revoked."
                : "Refresh token has expired.";
            return RefreshTokenRotationResult.Failure(reason);
        }

        // Revoke consumed token immediately before issuing the replacement.
        entry.IsRevoked = true;
        await StoreAsync(token, entry, cancellationToken).ConfigureAwait(false);

        // Issue replacement.
        var newToken = await GenerateAsync(entry.Subject, cancellationToken).ConfigureAwait(false);
        return RefreshTokenRotationResult.Success(newToken, entry.Subject);
    }

    /// <inheritdoc/>
    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var entry = await LoadAsync(token, cancellationToken).ConfigureAwait(false);
        if (entry is null) return;

        entry.IsRevoked = true;
        await StoreAsync(token, entry, cancellationToken).ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<RefreshTokenEntry?> LoadAsync(string token, CancellationToken ct)
    {
        var bytes = await _cache.GetAsync(KeyPrefix + token, ct).ConfigureAwait(false);
        return bytes is null ? null : JsonSerializer.Deserialize<RefreshTokenEntry>(bytes);
    }

    private async Task StoreAsync(string token, RefreshTokenEntry entry, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(entry);
        await _cache.SetAsync(KeyPrefix + token, bytes,
            new DistributedCacheEntryOptions { AbsoluteExpiration = entry.ExpiresAt },
            ct).ConfigureAwait(false);
    }

    private static string CreateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
                      .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
