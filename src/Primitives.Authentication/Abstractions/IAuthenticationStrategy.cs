namespace Primitives.Authentication.Abstractions;

/// <summary>
/// Defines the contract for an authentication strategy.
/// Implement this interface to create a new pluggable authentication mechanism.
/// </summary>
public interface IAuthenticationStrategy
{
    /// <summary>
    /// Unique name identifying this strategy (e.g. "OIDC", "Kerberos", "ApiKey").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether this strategy can authenticate with its current configuration.
    /// </summary>
    Task<bool> CanHandleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the authentication flow and returns the result.
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default);
}
