using Microsoft.AspNetCore.Http;
using Primitives.RateLimiting.Abstractions;

namespace Primitives.RateLimiting.Internal;

/// <summary>Default key provider that uses the remote IP address as the rate-limit bucket key.</summary>
internal sealed class RemoteIpKeyProvider : IRateLimitKeyProvider
{
    public Task<string> GetKeyAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return Task.FromResult(ip);
    }
}
