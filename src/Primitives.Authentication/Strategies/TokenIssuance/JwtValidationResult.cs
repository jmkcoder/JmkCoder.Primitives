using System.Security.Claims;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>Result returned by <see cref="IJwtTokenValidator.ValidateAsync"/>.</summary>
public sealed class JwtValidationResult
{
    /// <summary>Whether the token passed all validation checks.</summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The claims principal extracted from the token, populated when <see cref="IsValid"/> is
    /// <see langword="true"/>.
    /// </summary>
    public ClaimsPrincipal? Principal { get; init; }

    /// <summary>Human-readable reason for failure, populated when <see cref="IsValid"/> is
    /// <see langword="false"/>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Creates a successful validation result.</summary>
    public static JwtValidationResult Success(ClaimsPrincipal principal) =>
        new() { IsValid = true, Principal = principal };

    /// <summary>Creates a failed validation result.</summary>
    public static JwtValidationResult Failure(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
