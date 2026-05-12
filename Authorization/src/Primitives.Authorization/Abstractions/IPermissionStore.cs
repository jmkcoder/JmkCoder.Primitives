using Primitives.Authorization.Models;

namespace Primitives.Authorization.Abstractions;

/// <summary>Reads and writes subject–role assignments and resource-level grants.</summary>
public interface IPermissionStore
{
    /// <summary>Returns all role names assigned to <paramref name="subjectId"/> within a tenant.</summary>
    Task<IReadOnlyList<string>> GetRolesAsync(string subjectId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Assigns <paramref name="roleName"/> to <paramref name="subjectId"/> within a tenant.</summary>
    Task AssignRoleAsync(string subjectId, string roleName, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Revokes <paramref name="roleName"/> from <paramref name="subjectId"/> within a tenant.</summary>
    Task RevokeRoleAsync(string subjectId, string roleName, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns all resource-level grants for <paramref name="subjectId"/>.</summary>
    Task<IReadOnlyList<ResourceGrant>> GetResourceGrantsAsync(string subjectId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Grants <paramref name="permission"/> on a specific resource to <paramref name="subjectId"/>.</summary>
    Task GrantResourceAsync(ResourceGrant grant, CancellationToken cancellationToken = default);

    /// <summary>Revokes a resource-level grant.</summary>
    Task RevokeResourceAsync(string subjectId, string permission, string resourceType, string resourceId, string tenantId, CancellationToken cancellationToken = default);
}
