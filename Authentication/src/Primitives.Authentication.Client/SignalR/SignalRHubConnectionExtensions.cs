using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Client.SignalR;

public static class SignalRHubConnectionExtensions
{
    /// <summary>
    /// Configures the SignalR <see cref="HubConnectionBuilder"/> to supply a fresh
    /// Bearer JWT (from <paramref name="tokenService"/>) as the
    /// <see cref="HttpConnectionOptions.AccessTokenProvider"/> for every connection
    /// attempt.
    /// </summary>
    /// <remarks>
    /// The token is refreshed on every reconnect so short-lived JWTs are handled
    /// automatically.
    ///
    /// Usage:
    /// <code>
    /// var connection = new HubConnectionBuilder()
    ///     .WithUrl("https://api.example.com/hubs/notifications",
    ///              options => options.UsePrimitivesAuthentication(tokenService, "OIDC"))
    ///     .WithAutomaticReconnect()
    ///     .Build();
    /// </code>
    /// </remarks>
    public static void UsePrimitivesAuthentication(
        this HttpConnectionOptions options,
        ITokenIssuanceService      tokenService,
        string                     strategyName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tokenService);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);

        options.AccessTokenProvider = async () =>
        {
            var result = await tokenService.AuthenticateAsync(strategyName)
                                           .ConfigureAwait(false);
            return result.IsSuccess ? result.AccessToken : null;
        };
    }

    /// <summary>
    /// Fluent overload that wires <see cref="UsePrimitivesAuthentication"/> inside
    /// a <see cref="HubConnectionBuilder.WithUrl"/> callback — keeps the builder chain clean.
    /// </summary>
    /// <example>
    /// <code>
    /// var connection = new HubConnectionBuilder()
    ///     .WithPrimitivesAuthentication("https://api.example.com/hubs/chat",
    ///                                   tokenService, "OIDC")
    ///     .WithAutomaticReconnect()
    ///     .Build();
    /// </code>
    /// </example>
    public static IHubConnectionBuilder WithPrimitivesAuthentication(
        this IHubConnectionBuilder builder,
        string                     hubUrl,
        ITokenIssuanceService      tokenService,
        string                     strategyName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hubUrl);

        return builder.WithUrl(hubUrl, options =>
            options.UsePrimitivesAuthentication(tokenService, strategyName));
    }
}
