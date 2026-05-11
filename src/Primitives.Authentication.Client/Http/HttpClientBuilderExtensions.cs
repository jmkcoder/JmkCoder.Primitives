using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Client.Http;

public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="AuthenticatingHandler"/> to the named <see cref="System.Net.Http.HttpClient"/>
    /// pipeline.  The handler will fetch a JWT from the strategy named
    /// <paramref name="strategyName"/> and attach it as <c>Authorization: Bearer</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddHttpClient("payments-api", c => c.BaseAddress = new Uri("https://payments.example.com"))
    ///         .AddPrimitivesAuthentication("OIDC");
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddPrimitivesAuthentication(
        this IHttpClientBuilder builder,
        string                  strategyName,
        string                  tokenPrefix = "Bearer",
        string                  headerName  = "Authorization")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);

        return builder.AddHttpMessageHandler(sp =>
        {
            var tokenService = sp.GetRequiredService<ITokenIssuanceService>();
            var logger       = sp.GetRequiredService<ILogger<AuthenticatingHandler>>();
            var opts         = new AuthenticatingHandlerOptions
            {
                StrategyName = strategyName,
                TokenPrefix  = tokenPrefix,
                HeaderName   = headerName,
            };
            return new AuthenticatingHandler(tokenService, opts, logger);
        });
    }
}
