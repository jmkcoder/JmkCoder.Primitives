using Primitives.Multitenancy.Models;

namespace Primitives.Multitenancy.Abstractions;

/// <summary>
/// Provides the <see cref="Tenant"/> resolved for the current request or execution context.
/// Returns <see langword="null"/> when no tenant has been resolved (e.g. outside a tenanted
/// request or when the resolver found no match).
/// </summary>
public interface ITenantAccessor
{
    Tenant? Tenant { get; }
}
