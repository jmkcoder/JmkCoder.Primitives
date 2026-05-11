using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Primitives.Authentication.Client.MessageQueue;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Client.Extensions;

public static class ClientAuthenticationServiceExtensions
{
    /// <summary>
    /// Registers client-side Primitives token-provisioning services:
    /// <list type="bullet">
    ///   <item><description><see cref="IMessageTokenAttacher"/> (singleton) — for message-queue producers.</description></item>
    /// </list>
    /// HttpClient registration: <see cref="Http.HttpClientBuilderExtensions.AddPrimitivesAuthentication"/>.<br/>
    /// gRPC registration: <see cref="Grpc.PrimitivesGrpcCredentials"/>.
    /// </summary>
    /// <remarks>
    /// <strong>Prerequisite:</strong> call
    /// <c>services.AddAuthentication().AddJwtTokenIssuance(...)</c> from
    /// <c>Primitives.Authentication</c> before this method so that
    /// <see cref="ITokenIssuanceService"/> is registered in DI.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown at first resolution time if <c>ITokenIssuanceService</c> is not registered.
    /// </exception>
    public static IServiceCollection AddPrimitivesClientAuthentication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Factory registration: gives an actionable error if the core stack is missing.
        services.TryAddSingleton<IMessageTokenAttacher>(sp =>
        {
            var tokenService = sp.GetService<ITokenIssuanceService>()
                ?? throw new InvalidOperationException(
                    "ITokenIssuanceService is not registered. " +
                    "Call services.AddAuthentication().AddJwtTokenIssuance(o => { ... }) " +
                    "from Primitives.Authentication before calling AddPrimitivesClientAuthentication().");

            var logger = sp.GetRequiredService<ILogger<MessageTokenAttacher>>();
            return new MessageTokenAttacher(tokenService, logger);
        });

        return services;
    }
}
