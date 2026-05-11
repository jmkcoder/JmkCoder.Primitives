using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;
using Primitives.Multitenancy.Models;

namespace Primitives.Multitenancy.Internal;

/// <summary>
/// Reads the current <see cref="Tenant"/> from <see cref="HttpContext.Items"/>,
/// where <see cref="Middleware.TenantResolutionMiddleware"/> stores it per-request.
/// </summary>
internal sealed class TenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _contextAccessor;

    public TenantAccessor(IHttpContextAccessor contextAccessor)
        => _contextAccessor = contextAccessor;

    public Tenant? Tenant =>
        _contextAccessor.HttpContext?.Items[Middleware.TenantResolutionMiddleware.TenantItemKey] as Tenant;
}
