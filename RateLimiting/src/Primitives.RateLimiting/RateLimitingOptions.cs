using Primitives.RateLimiting.Models;

namespace Primitives.RateLimiting;

/// <summary>Top-level configuration for the rate-limiting module.</summary>
public sealed class RateLimitingOptions
{
    /// <summary>Named policies. At least one policy is required.</summary>
    public List<RateLimitPolicy> Policies { get; set; } = [];

    /// <summary>
    /// HTTP status code returned by the middleware when a request is throttled.
    /// Defaults to <c>429 Too Many Requests</c>.
    /// </summary>
    public int RejectionStatusCode { get; set; } = 429;

    /// <summary>
    /// When <see langword="true"/>, adds <c>X-RateLimit-*</c> and <c>Retry-After</c> headers
    /// to every response. Defaults to <see langword="true"/>.
    /// </summary>
    public bool AddRateLimitHeaders { get; set; } = true;
}
