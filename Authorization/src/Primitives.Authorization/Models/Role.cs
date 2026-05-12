namespace Primitives.Authorization.Models;

/// <summary>Defines a named role and the set of permissions it grants within a tenant.</summary>
public sealed class Role
{
    /// <summary>Unique name of the role (e.g. <c>"admin"</c>, <c>"viewer"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Tenant the role is scoped to.</summary>
    public required string TenantId { get; init; }

    /// <summary>Set of permission strings granted by this role (e.g. <c>"invoices:read"</c>).</summary>
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; init; }
}
