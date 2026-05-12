using System.Collections.Concurrent;
using Primitives.Authorization.Abstractions;
using Primitives.Authorization.Models;

namespace Primitives.Authorization.Internal;

/// <summary>Thread-safe in-memory permission store for subject–role assignments and resource grants.</summary>
internal sealed class InMemoryPermissionStore : IPermissionStore
{
    // Key: tenantId:subjectId → set of role names
    private readonly ConcurrentDictionary<string, HashSet<string>> _assignments =
        new(StringComparer.OrdinalIgnoreCase);

    // Key: tenantId:subjectId → list of resource grants
    private readonly ConcurrentDictionary<string, List<ResourceGrant>> _grants =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public Task<IReadOnlyList<string>> GetRolesAsync(string subjectId, string tenantId, CancellationToken cancellationToken = default)
    {
        _assignments.TryGetValue(SubjectKey(tenantId, subjectId), out var roles);
        return Task.FromResult<IReadOnlyList<string>>(roles?.ToList() ?? []);
    }

    public Task AssignRoleAsync(string subjectId, string roleName, string tenantId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var set = _assignments.GetOrAdd(SubjectKey(tenantId, subjectId), _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            set.Add(roleName);
        }
        return Task.CompletedTask;
    }

    public Task RevokeRoleAsync(string subjectId, string roleName, string tenantId, CancellationToken cancellationToken = default)
    {
        if (_assignments.TryGetValue(SubjectKey(tenantId, subjectId), out var set))
        {
            lock (_lock)
                set.Remove(roleName);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ResourceGrant>> GetResourceGrantsAsync(string subjectId, string tenantId, CancellationToken cancellationToken = default)
    {
        _grants.TryGetValue(SubjectKey(tenantId, subjectId), out var grants);
        return Task.FromResult<IReadOnlyList<ResourceGrant>>(grants?.ToList() ?? []);
    }

    public Task GrantResourceAsync(ResourceGrant grant, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var list = _grants.GetOrAdd(SubjectKey(grant.TenantId, grant.SubjectId), _ => []);
            list.RemoveAll(g => Matches(g, grant.SubjectId, grant.Permission, grant.ResourceType, grant.ResourceId, grant.TenantId));
            list.Add(grant);
        }
        return Task.CompletedTask;
    }

    public Task RevokeResourceAsync(string subjectId, string permission, string resourceType, string resourceId, string tenantId, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(SubjectKey(tenantId, subjectId), out var list))
        {
            lock (_lock)
                list.RemoveAll(g => Matches(g, subjectId, permission, resourceType, resourceId, tenantId));
        }
        return Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string SubjectKey(string tenantId, string subjectId) => $"{tenantId}:{subjectId}";

    private static bool Matches(ResourceGrant g, string subjectId, string permission, string resourceType, string resourceId, string tenantId) =>
        string.Equals(g.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(g.Permission, permission, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(g.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(g.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(g.TenantId, tenantId, StringComparison.OrdinalIgnoreCase);
}
