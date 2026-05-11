using Microsoft.Extensions.Options;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Tests.TokenIssuance;

public class JwtTokenValidatorTests
{
    private static JwtOptions DefaultOptions() => new()
    {
        Issuer    = "https://test.example.com",
        Audience  = "https://api.example.com",
        SigningKey = "super-secret-signing-key-that-is-long-enough",
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
    };

    private static (JwtTokenService Service, JwtTokenValidator Validator) Create(JwtOptions? opts = null)
    {
        var options   = Options.Create(opts ?? DefaultOptions());
        var time      = TimeProvider.System;
        var service   = new JwtTokenService(options, time);
        var validator = new JwtTokenValidator(options);
        return (service, validator);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_ForFreshToken()
    {
        var (svc, validator) = Create();
        var (token, _)       = svc.GenerateAccessToken("alice");

        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Principal);
        var sub = result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
               ?? result.Principal.FindFirst("sub");
        Assert.Equal("alice", sub?.Value);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_ForTamperedToken()
    {
        var (svc, validator) = Create();
        var (token, _)       = svc.GenerateAccessToken("alice");

        var tampered = token[..^5] + "XXXXX";
        var result   = await validator.ValidateAsync(tampered);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_ForWrongKey()
    {
        var opts = DefaultOptions();
        var (svc, _) = Create(opts);
        var (token, _) = svc.GenerateAccessToken("alice");

        // Validator with different key
        var otherOpts = DefaultOptions();
        otherOpts.SigningKey = "completely-different-key-that-is-also-long!!";
        var wrongValidator = new JwtTokenValidator(Options.Create(otherOpts));

        var result = await wrongValidator.ValidateAsync(token);
        Assert.False(result.IsValid);
    }
}
