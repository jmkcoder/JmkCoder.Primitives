namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// An issued refresh token record stored by <see cref="IRefreshTokenStore"/>.
/// </summary>
public sealed class RefreshTokenEntry
{
    public string Token { get; init; } = string.Empty;

    /// <summary>Identifies the principal this token was issued for (the JWT <c>sub</c> value).</summary>
    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Set to <see langword="true"/> when the token has been rotated or explicitly revoked.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Token that replaced this one during rotation (used for reuse-detection chain revocation).</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Whether this token is still valid for use at the given point in time.
    /// Callers must supply the current time (from an injected <see cref="TimeProvider"/>)
    /// rather than reading the system clock directly, making this testable.
    /// </summary>
    public bool IsActiveAt(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;
}
