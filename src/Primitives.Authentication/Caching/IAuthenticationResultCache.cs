using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Caching;

/// <summary>
/// Caches <see cref="AuthenticationResult"/> objects to avoid re-authenticating
/// on every request when the access token is still valid.
/// </summary>
public interface IAuthenticationResultCache
{
    /// <summary>
    /// Tries to retrieve a cached <see cref="AuthenticationResult"/> for the given
    /// <paramref name="cacheKey"/>. Returns <see langword="null"/> on a cache miss.
    /// </summary>
    Task<AuthenticationResult?> GetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a successful <see cref="AuthenticationResult"/> in the cache.
    /// The entry is automatically evicted when the token approaches expiry
    /// (minus the configured <see cref="AuthenticationCacheOptions.EarlyExpiryBuffer"/>).
    /// </summary>
    Task SetAsync(
        string cacheKey,
        AuthenticationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly removes a cache entry (e.g. after a token is known to be revoked).
    /// </summary>
    Task RemoveAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);
}
