using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Client.Grpc;

/// <summary>
/// Provides factory methods for attaching Primitives-issued JWTs to outgoing gRPC calls.
/// </summary>
public static class PrimitivesGrpcCredentials
{
    /// <summary>
    /// Creates <see cref="CallCredentials"/> that inject a JWT from
    /// <paramref name="tokenService"/> into every gRPC call's metadata.
    /// </summary>
    /// <remarks>
    /// <c>CallCredentials</c> require a TLS channel (<c>https://</c>).
    /// For insecure / development channels use
    /// <see cref="CreateInterceptor"/> instead.
    ///
    /// Usage:
    /// <code>
    /// var channel = GrpcChannel.ForAddress("https://grpc.example.com", new GrpcChannelOptions
    /// {
    ///     Credentials = ChannelCredentials.Create(
    ///         new SslCredentials(),
    ///         PrimitivesGrpcCredentials.Create(tokenService, "OIDC"))
    /// });
    /// </code>
    /// </remarks>
    public static CallCredentials Create(
        ITokenIssuanceService tokenService,
        string                strategyName)
    {
        ArgumentNullException.ThrowIfNull(tokenService);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);

        return CallCredentials.FromInterceptor(async (_, metadata) =>
        {
            var result = await tokenService.AuthenticateAsync(strategyName)
                                           .ConfigureAwait(false);
            if (result.IsSuccess && result.AccessToken is { Length: > 0 } token)
                metadata.Add("Authorization", $"Bearer {token}");
        });
    }

    /// <summary>
    /// Creates a <see cref="AuthenticatingClientInterceptor"/> that can be used with
    /// insecure (<c>http://</c>) gRPC channels or when you need interceptor-level control.
    /// </summary>
    /// <example>
    /// <code>
    /// var channel   = GrpcChannel.ForAddress("http://localhost:5001");
    /// var intercept = channel.Intercept(
    ///     PrimitivesGrpcCredentials.CreateInterceptor(tokenService, "OIDC"));
    /// var client    = new MyService.MyServiceClient(intercept);
    /// </code>
    /// </example>
    public static AuthenticatingClientInterceptor CreateInterceptor(
        ITokenIssuanceService tokenService,
        string                strategyName)
    {
        ArgumentNullException.ThrowIfNull(tokenService);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);
        return new AuthenticatingClientInterceptor(tokenService, strategyName);
    }
}
