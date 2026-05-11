namespace Primitives.Multitenancy.Models;

/// <summary>Represents a tenant in the system.</summary>
public sealed class Tenant
{
    /// <summary>
    /// The unique identifier used to look up this tenant.
    /// Must match what <see cref="Abstractions.ITenantResolver"/> returns.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Optional display name for the tenant.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Arbitrary per-tenant metadata (e.g. database connection strings, feature flags,
    /// plan information). Values are strings; deserialise as needed.
    /// </summary>
    public IReadOnlyDictionary<string, string> Claims { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
