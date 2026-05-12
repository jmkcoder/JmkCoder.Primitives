using Microsoft.AspNetCore.Http;

namespace Primitives.RateLimiting.Abstractions;

/// <summary>
/// Derives the rate-limit bucket key from an HTTP request.
/// The default implementation uses the remote IP address.
/// Register a custom implementation to key by tenant, user, API key, etc.
/// </summary>
public interface IRateLimitKeyProvider
{
    /// <summary>Returns the bucket key for the given request.</summary>
    Task<string> GetKeyAsync(HttpContext context, CancellationToken cancellationToken = default);
}
