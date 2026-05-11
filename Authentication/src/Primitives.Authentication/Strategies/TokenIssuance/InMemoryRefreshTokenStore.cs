using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Thread-safe, in-process refresh token store.
/// Suitable for single-instance deployments and testing.
/// For distributed / multi-instance deployments, implement <see cref="IRefreshTokenStore"/>
/// backed by Redis, SQL, or another shared store.
/// </summary>
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new();
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;
    private readonly object _rotateLock = new();

    public InMemoryRefreshTokenStore(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time    = time;
    }

    /// <inheritdoc/>
    public Task<string> GenerateAsync(string subject, CancellationToken cancellationToken = default)
    {
        PurgeStaleTokens();

        var now   = _time.GetUtcNow();
        var token = CreateSecureToken();
        _tokens[token] = new RefreshTokenEntry
        {
            Token     = token,
            Subject   = subject,
            CreatedAt = now,
            ExpiresAt = now.Add(_options.RefreshTokenLifetime)
        };

        return Task.FromResult(token);
    }

    /// <inheritdoc/>
    public Task<RefreshTokenRotationResult> ValidateAndRotateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // Serialize the check-and-swap to prevent concurrent rotation of the same token.
        lock (_rotateLock)
        {
            if (!_tokens.TryGetValue(token, out var entry))
                return Task.FromResult(RefreshTokenRotationResult.Failure("Refresh token not found."));

            var now      = _time.GetUtcNow();
            var isActive = !entry.IsRevoked && now < entry.ExpiresAt;

            if (!isActive)
            {
                // Token reuse detected — revoke the entire successor chain as a security measure.
                if (entry.ReplacedByToken is not null)
                    RevokeChain(entry.ReplacedByToken);

                var reason = entry.IsRevoked ? "Refresh token has been revoked." : "Refresh token has expired.";
                return Task.FromResult(RefreshTokenRotationResult.Failure(reason));
            }

            // Rotate: revoke old token, issue a new one.
            var newToken = CreateSecureToken();
            entry.IsRevoked       = true;
            entry.ReplacedByToken = newToken;

            _tokens[newToken] = new RefreshTokenEntry
            {
                Token     = newToken,
                Subject   = entry.Subject,
                CreatedAt = now,
                ExpiresAt = now.Add(_options.RefreshTokenLifetime)
            };

            return Task.FromResult(RefreshTokenRotationResult.Success(newToken, entry.Subject));
        }
    }

    /// <inheritdoc/>
    public Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(token, out var entry))
            entry.IsRevoked = true;

        return Task.CompletedTask;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void RevokeChain(string token)
    {
        if (_tokens.TryGetValue(token, out var entry) && !entry.IsRevoked)
        {
            entry.IsRevoked = true;
            if (entry.ReplacedByToken is not null)
                RevokeChain(entry.ReplacedByToken);
        }
    }

    /// <summary>Removes tokens that are both inactive and well past their expiry (saves memory).</summary>
    private void PurgeStaleTokens()
    {
        var cutoff = _time.GetUtcNow().AddDays(-1);
        var stale = _tokens
            .Where(kvp => kvp.Value.IsRevoked && kvp.Value.ExpiresAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in stale)
            _tokens.TryRemove(key, out _);
    }

    /// <summary>Generates a cryptographically random, URL-safe Base-64 token string.</summary>
    private static string CreateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
               .Replace('+', '-')
               .Replace('/', '_')
               .TrimEnd('=');
}
