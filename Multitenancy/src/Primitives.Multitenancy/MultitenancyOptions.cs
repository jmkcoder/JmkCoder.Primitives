using Primitives.Multitenancy.Models;

namespace Primitives.Multitenancy;

/// <summary>Configuration options for <c>Primitives.Multitenancy</c>.</summary>
public sealed class MultitenancyOptions
{
    /// <summary>
    /// When <see langword="true"/>, requests that cannot be resolved to a tenant are rejected
    /// with <see cref="TenantNotFoundStatusCode"/> before reaching application code.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool RequireTenant { get; set; } = false;

    /// <summary>
    /// HTTP status code returned when <see cref="RequireTenant"/> is <see langword="true"/>
    /// and no tenant can be resolved. Defaults to <c>400</c>.
    /// </summary>
    public int TenantNotFoundStatusCode { get; set; } = 400;

    /// <summary>
    /// Tenants used by <see cref="Internal.InMemoryTenantStore"/>.
    /// Ignored when a custom <see cref="Abstractions.ITenantStore"/> is registered.
    /// </summary>
    public List<Tenant> Tenants { get; set; } = new();
}
