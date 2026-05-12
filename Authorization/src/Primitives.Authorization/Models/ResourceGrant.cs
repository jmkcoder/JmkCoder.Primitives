namespace Primitives.Authorization.Models;

/// <summary>A permission grant scoped to a specific resource instance.</summary>
public sealed class ResourceGrant
{
    /// <summary>Subject (user/service) receiving the grant.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Permission string being granted (e.g. <c>"documents:edit"</c>).</summary>
    public required string Permission { get; init; }

    /// <summary>Type of the resource (e.g. <c>"document"</c>, <c>"project"</c>).</summary>
    public required string ResourceType { get; init; }

    /// <summary>Identifier of the specific resource instance.</summary>
    public required string ResourceId { get; init; }

    /// <summary>Tenant the grant is scoped to.</summary>
    public required string TenantId { get; init; }
}
