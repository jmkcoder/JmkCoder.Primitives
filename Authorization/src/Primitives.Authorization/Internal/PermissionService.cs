using Primitives.Authorization.Abstractions;

namespace Primitives.Authorization.Internal;

/// <summary>Default implementation of <see cref="IPermissionService"/>.</summary>
internal sealed class PermissionService : IPermissionService
{
    private readonly IPermissionStore _permissionStore;
    private readonly IRoleStore _roleStore;

    public PermissionService(IPermissionStore permissionStore, IRoleStore roleStore)
    {
        _permissionStore = permissionStore;
        _roleStore       = roleStore;
    }

    public async Task<bool> HasPermissionAsync(
        string subjectId,
        string permission,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(subjectId, tenantId, cancellationToken).ConfigureAwait(false);
        return permissions.Contains(permission);
    }

    public async Task<bool> HasPermissionOnResourceAsync(
        string subjectId,
        string permission,
        string resourceType,
        string resourceId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        // Check resource-level grants first
        var grants = await _permissionStore.GetResourceGrantsAsync(subjectId, tenantId, cancellationToken).ConfigureAwait(false);
        var hasResourceGrant = grants.Any(g =>
            string.Equals(g.Permission, permission, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(g.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(g.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase));

        if (hasResourceGrant)
            return true;

        // Fall back to tenant-level permission
        return await HasPermissionAsync(subjectId, permission, tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        string subjectId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var roleNames = await _permissionStore.GetRolesAsync(subjectId, tenantId, cancellationToken).ConfigureAwait(false);

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roleNames)
        {
            var role = await _roleStore.FindAsync(roleName, tenantId, cancellationToken).ConfigureAwait(false);
            if (role is not null)
                permissions.UnionWith(role.Permissions);
        }

        return permissions;
    }
}
