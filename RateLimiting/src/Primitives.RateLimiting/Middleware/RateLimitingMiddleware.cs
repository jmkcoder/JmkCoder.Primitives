using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.RateLimiting.Abstractions;

namespace Primitives.RateLimiting.Middleware;

/// <summary>
/// ASP.NET Core middleware that enforces a named rate-limit policy on every request.
/// The bucket key defaults to the remote IP address; override by implementing
/// <see cref="IRateLimitKeyProvider"/> and registering it in DI.
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly IRateLimitKeyProvider _keyProvider;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly string _policy;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimiter rateLimiter,
        IRateLimitKeyProvider keyProvider,
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger,
        string policy)
    {
        _next        = next;
        _rateLimiter = rateLimiter;
        _keyProvider = keyProvider;
        _options     = options.Value;
        _logger      = logger;
        _policy      = policy;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key    = await _keyProvider.GetKeyAsync(context, context.RequestAborted).ConfigureAwait(false);
        var result = await _rateLimiter.AcquireAsync(_policy, key, context.RequestAborted).ConfigureAwait(false);

        if (_options.AddRateLimitHeaders)
        {
            context.Response.Headers["X-RateLimit-Limit"]     = result.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"]     = ((long)result.RetryAfter.TotalSeconds).ToString();
        }

        if (!result.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for key '{Key}' on policy '{Policy}'", key, _policy);

            if (_options.AddRateLimitHeaders)
                context.Response.Headers["Retry-After"] = ((long)result.RetryAfter.TotalSeconds).ToString();

            context.Response.StatusCode = _options.RejectionStatusCode;
            return;
        }

        await _next(context);
    }
}
