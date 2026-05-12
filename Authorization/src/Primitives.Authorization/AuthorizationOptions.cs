using Primitives.Authorization.Models;

namespace Primitives.Authorization;

/// <summary>Top-level configuration for the authorization module.</summary>
public sealed class AuthorizationOptions
{
    /// <summary>
    /// Pre-seeded role definitions applied to the in-memory store on startup.
    /// Ignored when a custom <see cref="Abstractions.IRoleStore"/> is registered.
    /// </summary>
    public List<Role> Roles { get; set; } = [];
}
