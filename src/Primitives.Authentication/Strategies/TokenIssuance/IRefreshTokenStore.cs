namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Stores and validates refresh tokens.
/// The default implementation is <see cref="InMemoryRefreshTokenStore"/>.
/// Replace with a database-backed implementation for production multi-instance deployments.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>Generates and persists a new refresh token for <paramref name="subject"/>.</summary>
    Task<string> GenerateAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the supplied token, rotates it (revokes old, stores new), and returns the result.
    /// If the token has already been used (reuse attack), the entire chain is revoked.
    /// </summary>
    Task<RefreshTokenRotationResult> ValidateAndRotateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Explicitly revokes a token, preventing further use.</summary>
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>Result of a <see cref="IRefreshTokenStore.ValidateAndRotateAsync"/> call.</summary>
public sealed class RefreshTokenRotationResult
{
    public bool IsValid { get; init; }

    /// <summary>The newly issued refresh token (only present when <see cref="IsValid"/> is <see langword="true"/>).</summary>
    public string? NewToken { get; init; }

    /// <summary>The authenticated subject extracted from the old token.</summary>
    public string? Subject { get; init; }

    public string? ErrorMessage { get; init; }

    public static RefreshTokenRotationResult Success(string newToken, string subject) =>
        new() { IsValid = true, NewToken = newToken, Subject = subject };

    public static RefreshTokenRotationResult Failure(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
