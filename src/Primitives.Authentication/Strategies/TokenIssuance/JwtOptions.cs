using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Configuration for JWT generation and refresh token lifetime.
/// Register via <c>builder.AddJwtTokenIssuance(o => { ... })</c>.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>JWT <c>iss</c> claim — identifies who issued the token, e.g. "https://myapp.example.com".</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>JWT <c>aud</c> claim — identifies the intended recipient, e.g. "https://myapi.example.com".</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Symmetric signing key used for HMAC-SHA256 (HS256).
    /// Must be at least 32 characters / 256 bits when UTF-8 encoded.
    /// Store this in a secrets manager — never hardcode in source.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Lifetime of JWT access tokens. Defaults to 15 minutes.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Lifetime of refresh tokens. Defaults to 7 days.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
