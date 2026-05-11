namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Validates JWTs issued by <see cref="IJwtTokenService"/>.
/// Intended for resource servers that need to verify inbound tokens
/// without making a network call to the token issuer.
/// </summary>
public interface IJwtTokenValidator
{
    /// <summary>
    /// Validates <paramref name="token"/> and returns the extracted
    /// <see cref="JwtValidationResult.Principal"/> on success.
    /// </summary>
    Task<JwtValidationResult> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default);
}
