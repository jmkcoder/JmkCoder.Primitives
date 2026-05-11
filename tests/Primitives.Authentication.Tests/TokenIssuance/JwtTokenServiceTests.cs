using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Tests.TokenIssuance;

public class JwtTokenServiceTests
{
    private static JwtOptions DefaultOptions() => new()
    {
        Issuer              = "https://test.example.com",
        Audience            = "https://api.example.com",
        SigningKey          = "super-secret-signing-key-that-is-long-enough",
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
    };

    private static JwtTokenService CreateService(JwtOptions? opts = null, DateTimeOffset? now = null)
    {
        var fakeTime = new FakeTimeProvider(now ?? DateTimeOffset.UtcNow);
        return new JwtTokenService(Options.Create(opts ?? DefaultOptions()), fakeTime);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        var svc = CreateService();
        var (token, _) = svc.GenerateAccessToken("alice");
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateAccessToken_ExpiryMatchesConfiguredLifetime()
    {
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(baseTime);
        var opts     = DefaultOptions();
        opts.AccessTokenLifetime = TimeSpan.FromMinutes(30);
        var svc = new JwtTokenService(Options.Create(opts), fakeTime);

        var (_, expiresAt) = svc.GenerateAccessToken("alice");

        Assert.Equal(baseTime.AddMinutes(30), expiresAt);
    }

    [Fact]
    public void GenerateAccessToken_TwoTokens_HaveDifferentJti()
    {
        var svc = CreateService();
        var (token1, _) = svc.GenerateAccessToken("alice");
        var (token2, _) = svc.GenerateAccessToken("alice");
        Assert.NotEqual(token1, token2);
    }
}
