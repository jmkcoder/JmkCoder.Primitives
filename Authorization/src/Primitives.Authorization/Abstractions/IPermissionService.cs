namespace Primitives.Authorization.Abstractions;

/// <summary>
/// Evaluates whether a principal holds a permission, optionally scoped to a tenant and resource.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="subjectId"/> holds
    /// <paramref name="permission"/> within the given tenant.
    /// </summary>
    Task<bool> HasPermissionAsync(
        string subjectId,
        string permission,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="subjectId"/> holds
    /// <paramref name="permission"/> on the specific resource identified by
    /// <paramref name="resourceType"/> and <paramref name="resourceId"/>.
    /// Falls back to tenant-level check when no resource-level grant exists.
    /// </summary>
    Task<bool> HasPermissionOnResourceAsync(
        string subjectId,
        string permission,
        string resourceType,
        string resourceId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all permissions held by <paramref name="subjectId"/> within the given tenant.</summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(
        string subjectId,
        string tenantId,
        CancellationToken cancellationToken = default);
}
