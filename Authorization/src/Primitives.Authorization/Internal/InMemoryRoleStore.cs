using System.Collections.Concurrent;
using Primitives.Authorization.Abstractions;
using Primitives.Authorization.Models;

namespace Primitives.Authorization.Internal;

/// <summary>Thread-safe in-memory role store.</summary>
internal sealed class InMemoryRoleStore : IRoleStore
{
    // Key: tenantId:roleName
    private readonly ConcurrentDictionary<string, Role> _roles = new(StringComparer.OrdinalIgnoreCase);

    public Task<Role?> FindAsync(string roleName, string tenantId, CancellationToken cancellationToken = default)
    {
        _roles.TryGetValue(Key(tenantId, roleName), out var role);
        return Task.FromResult(role);
    }

    public Task<IReadOnlyList<Role>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var roles = _roles.Values.Where(r => string.Equals(r.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<Role>>(roles);
    }

    public Task UpsertAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles[Key(role.TenantId, role.Name)] = role;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string roleName, string tenantId, CancellationToken cancellationToken = default)
    {
        _roles.TryRemove(Key(tenantId, roleName), out _);
        return Task.CompletedTask;
    }

    private static string Key(string tenantId, string roleName) => $"{tenantId}:{roleName}";
}
