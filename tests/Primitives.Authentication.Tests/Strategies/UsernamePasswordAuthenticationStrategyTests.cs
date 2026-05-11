using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Primitives.Authentication.Strategies.UsernamePassword;

namespace Primitives.Authentication.Tests.Strategies;

public class UsernamePasswordAuthenticationStrategyTests
{
    private static UsernamePasswordAuthenticationStrategy Create(
        UsernamePasswordAuthenticationOptions opts,
        string name = "UsernamePassword")
    {
        var monitor = Substitute.For<IOptionsMonitor<UsernamePasswordAuthenticationOptions>>();
        monitor.Get(name).Returns(opts);
        return new UsernamePasswordAuthenticationStrategy(
            name, monitor, NullLogger<UsernamePasswordAuthenticationStrategy>.Instance);
    }

    [Fact]
    public async Task CanHandleAsync_ReturnsFalse_WhenCredentialsMissing()
    {
        Assert.False(await Create(new UsernamePasswordAuthenticationOptions()).CanHandleAsync());
        Assert.False(await Create(new UsernamePasswordAuthenticationOptions { Username = "u" }).CanHandleAsync());
        Assert.False(await Create(new UsernamePasswordAuthenticationOptions { Password = "p" }).CanHandleAsync());
    }

    [Fact]
    public async Task CanHandleAsync_ReturnsTrue_WhenBothSet()
    {
        var s = Create(new UsernamePasswordAuthenticationOptions { Username = "user", Password = "pass" });
        Assert.True(await s.CanHandleAsync());
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsBase64BasicCredentials()
    {
        var s = Create(new UsernamePasswordAuthenticationOptions { Username = "alice", Password = "secret" });
        var result = await s.AuthenticateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Basic", result.TokenType);
        Assert.Equal("alice", result.Subject);

        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result.AccessToken!));
        Assert.Equal("alice:secret", decoded);
    }

    [Fact]
    public void Name_ReturnsInjectedName()
    {
        var s = Create(new UsernamePasswordAuthenticationOptions(), "MyBasic");
        Assert.Equal("MyBasic", s.Name);
    }
}
