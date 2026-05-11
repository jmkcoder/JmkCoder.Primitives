using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Caching;

/// <summary>
/// Cache configuration consumed by <see cref="InMemoryAuthenticationResultCache"/>.
/// </summary>
public sealed class AuthenticationCacheOptions
{
    /// <summary>
    /// How far before a token's actual expiry it should be considered expired in the cache.
    /// Defaults to 30 seconds to account for clock skew and network latency.
    /// </summary>
    public TimeSpan EarlyExpiryBuffer { get; set; } = TimeSpan.FromSeconds(30);
}
