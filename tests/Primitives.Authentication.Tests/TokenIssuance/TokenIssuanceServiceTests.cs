using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Factory;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Tests.TokenIssuance;

public class TokenIssuanceServiceTests
{
    private static JwtOptions JwtOpts() => new()
    {
        Issuer              = "https://test.example.com",
        Audience            = "https://api.example.com",
        SigningKey          = "super-secret-signing-key-that-is-long-enough",
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(7),
    };

    private static (TokenIssuanceService Svc, IAuthenticationStrategyFactory Factory) Create(
        IAuthenticationStrategy strategy,
        string strategyName = "TestStrategy")
    {
        var mockStrategy = strategy;
        mockStrategy.Name.Returns(strategyName);

        var factory  = new AuthenticationStrategyFactory([mockStrategy]);
        var time     = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var opts     = Options.Create(JwtOpts());
        var jwtSvc   = new JwtTokenService(opts, time);
        var store    = new InMemoryRefreshTokenStore(opts, time);
        var svc      = new TokenIssuanceService(
            factory, jwtSvc, store,
            NullLogger<TokenIssuanceService>.Instance);
        return (svc, factory);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsJwt_WhenStrategySucceeds()
    {
        var strategy = Substitute.For<IAuthenticationStrategy>();
        strategy.Name.Returns("TestStrategy");
        strategy.AuthenticateAsync(Arg.Any<CancellationToken>())
                .Returns(AuthenticationResult.Success("upstream-token", "Bearer",
                    subject: "alice", claims: null));

        var (svc, _) = Create(strategy);
        var result   = await svc.AuthenticateAsync("TestStrategy");

        Assert.True(result.IsSuccess);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal("alice", result.Subject);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        // The JWT should be different from the upstream token
        Assert.NotEqual("upstream-token", result.AccessToken);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsFailure_WhenStrategyFails()
    {
        var strategy = Substitute.For<IAuthenticationStrategy>();
        strategy.Name.Returns("TestStrategy");
        strategy.AuthenticateAsync(Arg.Any<CancellationToken>())
                .Returns(AuthenticationResult.Failure("Credentials invalid"));

        var (svc, _) = Create(strategy);
        var result   = await svc.AuthenticateAsync("TestStrategy");

        Assert.False(result.IsSuccess);
        Assert.Equal("Credentials invalid", result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNewJwt_WhenTokenValid()
    {
        var strategy = Substitute.For<IAuthenticationStrategy>();
        strategy.Name.Returns("TestStrategy");
        strategy.AuthenticateAsync(Arg.Any<CancellationToken>())
                .Returns(AuthenticationResult.Success("token", "Bearer", subject: "bob"));

        var (svc, _) = Create(strategy);

        // Authenticate first to get a refresh token
        var auth     = await svc.AuthenticateAsync("TestStrategy");
        Assert.True(auth.IsSuccess);

        var refresh  = await svc.RefreshAsync(auth.RefreshToken!);

        Assert.True(refresh.IsSuccess);
        Assert.Equal("bob", refresh.Subject);
        Assert.NotNull(refresh.RefreshToken);
        Assert.NotEqual(auth.RefreshToken, refresh.RefreshToken); // rotated
    }

    [Fact]
    public async Task RefreshAsync_ReturnsFailure_ForUnknownRefreshToken()
    {
        var strategy = Substitute.For<IAuthenticationStrategy>();
        strategy.Name.Returns("TestStrategy");
        var (svc, _) = Create(strategy);

        var result = await svc.RefreshAsync("not-a-real-refresh-token");
        Assert.False(result.IsSuccess);
    }
}
