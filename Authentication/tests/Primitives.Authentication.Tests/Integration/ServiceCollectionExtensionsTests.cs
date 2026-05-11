using Microsoft.Extensions.DependencyInjection;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Extensions;
using Primitives.Authentication.Factory;
using Primitives.Authentication.Strategies.ApiKey;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Tests.Integration;

/// <summary>
/// Integration tests that verify the DI container wires everything up correctly.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAuthentication_RegistersCoreInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
                .AddApiKey(o => o.ApiKey = "key");

        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IAuthenticationStrategyFactory>();
        Assert.NotNull(factory);
        Assert.Contains("ApiKey", factory.RegisteredStrategyNames);
    }

    [Fact]
    public void AddMultipleStrategies_AllRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
                .AddApiKey("Key1", o => o.ApiKey = "k1")
                .AddApiKey("Key2", o => o.ApiKey = "k2");

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IAuthenticationStrategyFactory>();

        Assert.Contains("Key1", factory.RegisteredStrategyNames);
        Assert.Contains("Key2", factory.RegisteredStrategyNames);
    }

    [Fact]
    public void AddJwtTokenIssuance_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
                .AddApiKey(o => o.ApiKey = "test-key")
                .AddJwtTokenIssuance(o =>
                {
                    o.Issuer    = "https://issuer.example.com";
                    o.Audience  = "https://audience.example.com";
                    o.SigningKey = "a-valid-signing-key-that-is-long-enough!!";
                });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IJwtTokenService>());
        Assert.NotNull(provider.GetRequiredService<IRefreshTokenStore>());
        Assert.NotNull(provider.GetRequiredService<IJwtTokenValidator>());
        Assert.NotNull(provider.GetRequiredService<ITokenIssuanceService>());
    }

    [Fact]
    public void AddResultCache_RegistersCacheService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
                .AddApiKey(o => o.ApiKey = "key")
                .AddResultCache();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<Caching.IAuthenticationResultCache>());
    }
}
