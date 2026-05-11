using Primitives.Multitenancy.Models;

namespace Primitives.Multitenancy.Abstractions;

/// <summary>
/// Looks up a <see cref="Tenant"/> by its resolved identifier.
/// Implement this interface to back tenant resolution with a database, remote API, or cache.
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Returns the <see cref="Tenant"/> for <paramref name="identifier"/>,
    /// or <see langword="null"/> if no such tenant exists.
    /// </summary>
    Task<Tenant?> FindByIdentifierAsync(string identifier, CancellationToken ct = default);
}
