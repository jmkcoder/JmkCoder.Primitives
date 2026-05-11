using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Creates HMAC-SHA256 signed JWTs using <see cref="JwtOptions"/> configuration.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time    = time;
    }

    /// <inheritdoc/>
    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(
        string subject,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now       = _time.GetUtcNow();
        var expiresAt = now.Add(_options.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        if (additionalClaims is not null)
            claims.AddRange(additionalClaims);

        var jwt = new JwtSecurityToken(
            issuer:             _options.Issuer,
            audience:           _options.Audience,
            claims:             claims,
            notBefore:          now.UtcDateTime,
            expires:            expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (_handler.WriteToken(jwt), expiresAt);
    }
}
