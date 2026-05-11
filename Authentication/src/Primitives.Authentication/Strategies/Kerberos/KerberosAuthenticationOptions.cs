using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.Kerberos;

/// <summary>
/// Configuration for Kerberos / Windows Negotiate authentication.
/// Uses <see cref="System.Net.Security.NegotiateAuthentication"/> (.NET 7+) to acquire
/// a Kerberos Service Ticket and returns it as a Negotiate token.
/// </summary>
public sealed class KerberosAuthenticationOptions
{
    /// <summary>
    /// Service Principal Name (SPN) of the target service, e.g. "HTTP/myservice.contoso.com".
    /// Required for Kerberos – this identifies which service ticket to acquire.
    /// </summary>
    [Required]
    public string ServicePrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// Optional explicit credential used for constrained/unconstrained delegation.
    /// When <see langword="null"/> the current process identity (Windows SSO) is used.
    /// </summary>
    public NetworkCredentialOptions? Credential { get; set; }

    /// <summary>
    /// SSPI/GSSAPI security package to use. Defaults to "Kerberos".
    /// Use "Negotiate" to allow automatic downgrade to NTLM when Kerberos is unavailable.
    /// </summary>
    public string Package { get; set; } = "Kerberos";
}

/// <summary>Explicit Windows / Kerberos network credential.</summary>
public sealed class NetworkCredentialOptions
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>Active Directory domain name, e.g. "CONTOSO".</summary>
    public string? Domain { get; set; }
}
