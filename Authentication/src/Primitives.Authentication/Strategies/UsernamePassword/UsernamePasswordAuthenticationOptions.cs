using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.UsernamePassword;

/// <summary>
/// Configuration for HTTP Basic Authentication (username + password).
/// The strategy encodes the credentials and returns a Basic Authorization header value.
/// </summary>
public sealed class UsernamePasswordAuthenticationOptions
{
    /// <summary>The username / login identifier.</summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>The plain-text password (stored only in memory; never serialised).</summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional realm included in the Authorization header per RFC 7617.
    /// When set the header becomes: Basic realm="value" {credentials}.
    /// Leave <see langword="null"/> to omit the realm.
    /// </summary>
    public string? Realm { get; set; }

    /// <summary>
    /// Text encoding used to convert the "username:password" string to bytes before Base-64 encoding.
    /// Defaults to UTF-8 as required by RFC 7617.
    /// </summary>
    public System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
}
