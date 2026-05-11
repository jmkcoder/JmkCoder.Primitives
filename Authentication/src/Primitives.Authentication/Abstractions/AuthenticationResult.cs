namespace Primitives.Authentication.Abstractions;

/// <summary>
/// Encapsulates the outcome of an authentication attempt.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>Whether authentication succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Bearer or negotiated access token on success.</summary>
    public string? AccessToken { get; init; }

    /// <summary>Token type, e.g. "Bearer" or "Negotiate".</summary>
    public string? TokenType { get; init; }

    /// <summary>UTC instant when the token expires, if known.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Additional claims or metadata returned by the provider.</summary>
    public IReadOnlyDictionary<string, string>? Claims { get; init; }

    /// <summary>
    /// Identifies the authenticated principal (e.g. username, clientId, SPN).
    /// Used by <c>ITokenIssuanceService</c> as the JWT <c>sub</c> claim.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Rolling refresh token issued alongside the JWT access token.
    /// Only present when <c>ITokenIssuanceService</c> is used.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>Human-readable error description when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Underlying exception when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public Exception? Exception { get; init; }

    private AuthenticationResult() { }

    /// <summary>Creates a successful result carrying the issued token.</summary>
    public static AuthenticationResult Success(
        string accessToken,
        string tokenType = "Bearer",
        DateTimeOffset? expiresAt = null,
        IReadOnlyDictionary<string, string>? claims = null,
        string? subject = null,
        string? refreshToken = null) =>
        new()
        {
            IsSuccess = true,
            AccessToken = accessToken,
            TokenType = tokenType,
            ExpiresAt = expiresAt,
            Claims = claims,
            Subject = subject,
            RefreshToken = refreshToken
        };

    /// <summary>Creates a failed result with the given error information.</summary>
    public static AuthenticationResult Failure(string errorMessage, Exception? exception = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };

    /// <summary>Formats the token as an HTTP Authorization header value, e.g. "Bearer eyJ...".</summary>
    public string? ToAuthorizationHeaderValue() =>
        AccessToken is null ? null : $"{TokenType} {AccessToken}";
}
