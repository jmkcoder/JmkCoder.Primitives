using System.Security.Claims;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Mints signed JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT for <paramref name="subject"/> with the configured issuer,
    /// audience, and lifetime.
    /// </summary>
    /// <param name="subject">The <c>sub</c> claim value (authenticated principal identifier).</param>
    /// <param name="additionalClaims">Any extra claims to embed in the token payload.</param>
    /// <returns>The compact-serialised JWT string and its expiry instant.</returns>
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(
        string subject,
        IEnumerable<Claim>? additionalClaims = null);
}
