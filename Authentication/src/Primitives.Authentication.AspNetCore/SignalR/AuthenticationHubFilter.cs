using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.AspNetCore.SignalR;

/// <summary>
/// SignalR hub filter that validates the Bearer token on every connection and
/// hub method invocation.
/// </summary>
/// <remarks>
/// Tokens are resolved in priority order:
/// <list type="number">
///   <item><description><c>Authorization: Bearer &lt;token&gt;</c> HTTP header</description></item>
///   <item><description><c>?access_token=&lt;token&gt;</c> query parameter (used by JS and native
///   SignalR clients during WebSocket / SSE upgrade)</description></item>
/// </list>
///
/// Register per-hub:
/// <code>
/// services.AddSignalR()
///         .AddHubOptions&lt;MyHub&gt;(o => o.AddFilter&lt;AuthenticationHubFilter&gt;());
/// services.AddSingleton&lt;AuthenticationHubFilter&gt;();
/// // or call services.AddPrimitivesAspNetCoreAuthentication()
/// </code>
/// </remarks>
public sealed class AuthenticationHubFilter : IHubFilter
{
    private readonly IJwtTokenValidator              _validator;
    private readonly ILogger<AuthenticationHubFilter> _logger;

    public AuthenticationHubFilter(
        IJwtTokenValidator               validator,
        ILogger<AuthenticationHubFilter> logger)
    {
        _validator = validator;
        _logger    = logger;
    }

    public async Task OnConnectedAsync(
        HubLifetimeContext              context,
        Func<HubLifetimeContext, Task>  next)
    {
        if (!await ValidateAsync(context.Context).ConfigureAwait(false))
            return;   // connection already aborted inside ValidateAsync

        await next(context).ConfigureAwait(false);
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext                           invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (!await ValidateAsync(invocationContext.Context).ConfigureAwait(false))
            return null;

        return await next(invocationContext).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------

    private async Task<bool> ValidateAsync(HubCallerContext context)
    {
        var token = ResolveToken(context);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning(
                "SignalR connection {ConnectionId}: no token present — aborting",
                context.ConnectionId);
            context.Abort();
            return false;
        }

        var result = await _validator.ValidateAsync(token, context.ConnectionAborted)
                                     .ConfigureAwait(false);
        if (!result.IsValid)
        {
            _logger.LogWarning(
                "SignalR connection {ConnectionId}: token invalid — {Error}",
                context.ConnectionId, result.ErrorMessage);
            context.Abort();
            return false;
        }

        return true;
    }

    private static string? ResolveToken(HubCallerContext context)
    {
        var http = context.GetHttpContext();

        // 1. Authorization header
        var header = http?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(header) &&
            header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer ".Length..];
        }

        // 2. Query string (WebSocket / SSE upgrade — JS SignalR client)
        var qs = http?.Request.Query["access_token"].ToString();
        return string.IsNullOrEmpty(qs) ? null : qs;
    }
}
