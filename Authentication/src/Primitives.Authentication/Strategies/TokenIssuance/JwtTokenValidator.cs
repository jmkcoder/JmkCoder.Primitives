using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Validates JWTs produced by <see cref="JwtTokenService"/> using the same
/// signing key and issuer/audience settings from <see cref="JwtOptions"/>.
/// </summary>
public sealed class JwtTokenValidator : IJwtTokenValidator
{
    private readonly JwtOptions _options;
    private readonly TokenValidationParameters _validationParams;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenValidator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _validationParams = BuildValidationParameters(_options);
    }

    /// <inheritdoc/>
    public Task<JwtValidationResult> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = _handler.ValidateToken(token, _validationParams, out _);
            return Task.FromResult(JwtValidationResult.Success(principal));
        }
        catch (SecurityTokenExpiredException ex)
        {
            return Task.FromResult(JwtValidationResult.Failure(string.Concat("Token expired: ", ex.Message)));
        }
        catch (SecurityTokenException ex)
        {
            return Task.FromResult(JwtValidationResult.Failure(string.Concat("Token validation failed: ", ex.Message)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JwtValidationResult.Failure(string.Concat("Unexpected validation error: ", ex.Message)));
        }
    }

    private static TokenValidationParameters BuildValidationParameters(JwtOptions options) =>
        new()
        {
            ValidateIssuer           = true,
            ValidIssuer              = options.Issuer,
            ValidateAudience         = true,
            ValidAudience            = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30),
        };
}