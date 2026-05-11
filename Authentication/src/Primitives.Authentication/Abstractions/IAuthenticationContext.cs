namespace Primitives.Authentication.Abstractions;

/// <summary>
/// Context that holds the active authentication strategy and delegates execution to it.
/// Allows switching strategies at runtime (classic Strategy pattern).
/// </summary>
public interface IAuthenticationContext
{
    /// <summary>Name of the currently active strategy.</summary>
    string ActiveStrategy { get; }

    /// <summary>Replaces the active strategy.</summary>
    void SetStrategy(IAuthenticationStrategy strategy);

    /// <summary>Authenticates using the currently active strategy.</summary>
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default);
}
