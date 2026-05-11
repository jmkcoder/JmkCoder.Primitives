using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Middleware;

/// <summary>
/// ASP.NET Core middleware that resolves the current tenant on every request.
/// Places the resolved <see cref="Models.Tenant"/> into <see cref="HttpContext.Items"/>
/// under <see cref="TenantItemKey"/> so that <see cref="Internal.TenantAccessor"/> can read it.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    /// <summary>Key under which the resolved tenant is stored in <see cref="HttpContext.Items"/>.</summary>
    public static readonly object TenantItemKey = new();

    private readonly RequestDelegate _next;
    private readonly ITenantResolver _resolver;
    private readonly ITenantStore _store;
    private readonly MultitenancyOptions _options;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ITenantResolver resolver,
        ITenantStore store,
        IOptions<MultitenancyOptions> options,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next     = next;
        _resolver = resolver;
        _store    = store;
        _options  = options.Value;
        _logger   = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identifier = await _resolver.ResolveAsync(context, context.RequestAborted);

        if (identifier is not null)
        {
            var tenant = await _store.FindByIdentifierAsync(identifier, context.RequestAborted);
            if (tenant is not null)
            {
                context.Items[TenantItemKey] = tenant;
                _logger.LogDebug("Tenant resolved: {TenantId}", tenant.Id);
            }
            else
            {
                _logger.LogDebug("Tenant identifier '{Identifier}' was resolved but not found in the store",
                    identifier);
            }
        }

        if (_options.RequireTenant && context.Items[TenantItemKey] is null)
        {
            _logger.LogWarning("Request rejected — no tenant could be resolved and RequireTenant is true");
            context.Response.StatusCode = _options.TenantNotFoundStatusCode;
            return;
        }

        await _next(context);
    }
}
