using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.Oidc;

/// <summary>
/// Configuration for OIDC / OAuth 2.0 authentication.
/// Supports two flows:
///   - Client Credentials (machine-to-machine, no user context)
///   - ROPC (Resource Owner Password Credentials, user + password via token endpoint)
/// </summary>
public sealed class OidcAuthenticationOptions
{
    /// <summary>OAuth2/OIDC authority URL, e.g. "https://login.microsoftonline.com/{tenantId}".</summary>
    [Required]
    public string Authority { get; set; } = string.Empty;

    /// <summary>Application (client) identifier registered with the identity provider.</summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret. Required for Client Credentials and ROPC flows.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Scopes to request, e.g. ["https://graph.microsoft.com/.default"].
    /// Defaults to ["{ClientId}/.default"] when empty.
    /// </summary>
    public IEnumerable<string> Scopes { get; set; } = [];

    /// <summary>
    /// Selects the OAuth2 flow. Defaults to <see cref="OidcFlow.ClientCredentials"/>.
    /// </summary>
    public OidcFlow Flow { get; set; } = OidcFlow.ClientCredentials;

    // ── ROPC-only properties ───────────────────────────────────────────────

    /// <summary>End-user username. Required when <see cref="Flow"/> is <see cref="OidcFlow.ResourceOwnerPassword"/>.</summary>
    public string? Username { get; set; }

    /// <summary>End-user password. Required when <see cref="Flow"/> is <see cref="OidcFlow.ResourceOwnerPassword"/>.</summary>
    public string? Password { get; set; }
}

/// <summary>Selects the OAuth2 grant type used by <see cref="OidcAuthenticationStrategy"/>.</summary>
public enum OidcFlow
{
    /// <summary>OAuth2 Client Credentials grant (machine-to-machine, no user context).</summary>
    ClientCredentials,

    /// <summary>OAuth2 Resource Owner Password Credentials grant (username + password delegated to token endpoint).</summary>
    ResourceOwnerPassword
}
