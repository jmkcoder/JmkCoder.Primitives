using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Strategies.ApiKey;

namespace Primitives.Authentication.Tests.Strategies;

public class ApiKeyAuthenticationStrategyTests
{
    private static ApiKeyAuthenticationStrategy Create(
        ApiKeyAuthenticationOptions opts,
        string name = "ApiKey")
    {
        var monitor = Substitute.For<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        monitor.Get(name).Returns(opts);
        return new ApiKeyAuthenticationStrategy(name, monitor, NullLogger<ApiKeyAuthenticationStrategy>.Instance);
    }

    [Fact]
    public void Name_ReturnsInjectedName()
    {
        var strategy = Create(new ApiKeyAuthenticationOptions { ApiKey = "k" }, "MyKey");
        Assert.Equal("MyKey", strategy.Name);
    }

    [Fact]
    public async Task CanHandleAsync_ReturnsFalse_WhenApiKeyEmpty()
    {
        var strategy = Create(new ApiKeyAuthenticationOptions());
        Assert.False(await strategy.CanHandleAsync());
    }

    [Fact]
    public async Task CanHandleAsync_ReturnsTrue_WhenApiKeySet()
    {
        var strategy = Create(new ApiKeyAuthenticationOptions { ApiKey = "my-key" });
        Assert.True(await strategy.CanHandleAsync());
    }

    [Theory]
    [InlineData(ApiKeyPlacement.BearerToken, "Bearer")]
    [InlineData(ApiKeyPlacement.QueryParameter, "X-API-Key")]
    public async Task AuthenticateAsync_ReturnsSuccess_WithCorrectTokenType(
        ApiKeyPlacement placement, string expectedTokenType)
    {
        var strategy = Create(new ApiKeyAuthenticationOptions
        {
            ApiKey    = "abc123",
            Placement = placement,
            KeyName   = "X-API-Key"
        });

        var result = await strategy.AuthenticateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("abc123", result.AccessToken);
        Assert.Equal(expectedTokenType, result.TokenType);
    }

    [Fact]
    public async Task AuthenticateAsync_Header_PrefixesKey()
    {
        var strategy = Create(new ApiKeyAuthenticationOptions
        {
            ApiKey       = "secret",
            Placement    = ApiKeyPlacement.Header,
            // Note: TrimEnd() strips trailing whitespace from the prefix,
            // so the separator must be included within the key value or not used at all.
            HeaderPrefix = "APIKey-",
            KeyName      = "X-API-Key"
        });

        var result = await strategy.AuthenticateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("APIKey-secret", result.AccessToken);
    }
}
