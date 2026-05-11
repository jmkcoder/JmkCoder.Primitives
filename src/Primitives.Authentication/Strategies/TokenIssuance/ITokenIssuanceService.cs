using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Orchestrates authentication + JWT issuance + rolling refresh token rotation.
/// Inject this instead of <see cref="IAuthenticationContext"/> when you need
/// self-contained JWT tokens regardless of the underlying credential type.
/// </summary>
public interface ITokenIssuanceService
{
    /// <summary>
    /// Runs the named strategy to verify the caller's identity, then mints
    /// a JWT access token and a rolling refresh token.
    /// </summary>
    /// <param name="strategyName">
    /// Case-insensitive name of a registered <see cref="IAuthenticationStrategy"/>
    /// (e.g. "OIDC", "UsernamePassword", "Kerberos", "ApiKey").
    /// </param>
    Task<AuthenticationResult> AuthenticateAsync(
        string strategyName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and rotates the supplied refresh token (rolling rotation),
    /// issuing a new JWT access token and a new refresh token.
    /// The old refresh token is revoked immediately after rotation.
    /// If the token has already been used, the entire successor chain is revoked
    /// (refresh token reuse detection).
    /// </summary>
    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
