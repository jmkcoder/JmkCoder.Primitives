using Primitives.Authorization.Models;

namespace Primitives.Authorization.Abstractions;

/// <summary>Reads and writes role definitions for a tenant.</summary>
public interface IRoleStore
{
    /// <summary>Returns the role with the given name within a tenant, or <see langword="null"/>.</summary>
    Task<Role?> FindAsync(string roleName, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns all roles defined for a tenant.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Persists a role definition (insert or replace).</summary>
    Task UpsertAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>Removes a role and all its subject assignments.</summary>
    Task DeleteAsync(string roleName, string tenantId, CancellationToken cancellationToken = default);
}
