using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Context;
using Primitives.Authentication.Factory;

namespace Primitives.Authentication.Extensions;

/// <summary>
/// Extension methods for registering Primitives.Authentication services into the
/// Microsoft Dependency Injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core authentication infrastructure (factory, context) and returns an
    /// <see cref="AuthenticationBuilder"/> for registering one or more strategies.
    ///
    /// <example>
    /// <code>
    /// services.AddAuthentication()
    ///     .AddOidc(o =>
    ///     {
    ///         o.Authority    = "https://login.microsoftonline.com/{tenantId}";
    ///         o.ClientId     = "your-client-id";
    ///         o.ClientSecret = "your-client-secret";
    ///     })
    ///     .AddKerberos(o => o.ServicePrincipalName = "HTTP/myservice.contoso.com")
    ///     .AddApiKey(o => o.ApiKey = "super-secret-key");
    /// </code>
    /// </example>
    /// </summary>
    public static AuthenticationBuilder AddAuthentication(this IServiceCollection services)
    {
        // System clock — can be overridden in tests via services.AddSingleton(FakeTimeProvider).
        services.TryAddSingleton(TimeProvider.System);

        // Core infrastructure — register only once even if called multiple times.
        services.TryAddSingleton<IAuthenticationStrategyFactory, AuthenticationStrategyFactory>();
        services.TryAddTransient<IAuthenticationContext>(sp =>
        {
            var factory = sp.GetRequiredService<IAuthenticationStrategyFactory>();
            var firstStrategy = factory.RegisteredStrategyNames.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "No authentication strategy is registered. " +
                    "Call AddOidc(), AddKerberos(), AddApiKey(), or AddUsernamePassword() " +
                    "on the AuthenticationBuilder.");

            return new AuthenticationContext(factory.GetStrategy(firstStrategy));
        });

        return new AuthenticationBuilder(services);
    }
}
