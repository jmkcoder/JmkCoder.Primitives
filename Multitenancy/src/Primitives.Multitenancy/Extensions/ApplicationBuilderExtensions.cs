using Microsoft.AspNetCore.Builder;
using Primitives.Multitenancy.Middleware;

namespace Primitives.Multitenancy.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="TenantResolutionMiddleware"/> to the request pipeline.
    /// Place this after <c>UseRouting</c> (if using route-value resolution) and
    /// after <c>UseAuthentication</c> (if using claim-based resolution), but before
    /// <c>UseAuthorization</c> and your endpoint handlers.
    /// </summary>
    public static IApplicationBuilder UsePrimitivesMultitenancy(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();
}
