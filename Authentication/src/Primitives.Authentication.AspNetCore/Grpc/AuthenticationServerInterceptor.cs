using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.AspNetCore.Grpc;

/// <summary>
/// gRPC server interceptor that validates the incoming Bearer token using
/// <see cref="IJwtTokenValidator"/>.  Rejects unauthenticated calls with
/// <see cref="StatusCode.Unauthenticated"/> before the handler is reached.
/// </summary>
/// <remarks>
/// Register with:
/// <code>
/// services.AddGrpc(o => o.Interceptors.Add&lt;AuthenticationServerInterceptor&gt;());
/// services.AddSingleton&lt;AuthenticationServerInterceptor&gt;();
/// // or call services.AddPrimitivesAspNetCoreAuthentication()
/// </code>
/// </remarks>
public sealed class AuthenticationServerInterceptor : Interceptor
{
    private readonly IJwtTokenValidator                        _validator;
    private readonly ILogger<AuthenticationServerInterceptor> _logger;

    public AuthenticationServerInterceptor(
        IJwtTokenValidator                        validator,
        ILogger<AuthenticationServerInterceptor> logger)
    {
        _validator = validator;
        _logger    = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest                                  request,
        ServerCallContext                          context,
        UnaryServerMethod<TRequest, TResponse>    continuation)
    {
        await AuthenticateAsync(context).ConfigureAwait(false);
        return await continuation(request, context).ConfigureAwait(false);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest                                          request,
        IServerStreamWriter<TResponse>                    responseStream,
        ServerCallContext                                 context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await AuthenticateAsync(context).ConfigureAwait(false);
        await continuation(request, responseStream, context).ConfigureAwait(false);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest>                       requestStream,
        ServerCallContext                                  context,
        ClientStreamingServerMethod<TRequest, TResponse>  continuation)
    {
        await AuthenticateAsync(context).ConfigureAwait(false);
        return await continuation(requestStream, context).ConfigureAwait(false);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest>                        requestStream,
        IServerStreamWriter<TResponse>                      responseStream,
        ServerCallContext                                   context,
        DuplexStreamingServerMethod<TRequest, TResponse>   continuation)
    {
        await AuthenticateAsync(context).ConfigureAwait(false);
        await continuation(requestStream, responseStream, context).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------

    private async Task AuthenticateAsync(ServerCallContext context)
    {
        var authEntry = context.RequestHeaders.Get("authorization");
        if (authEntry is null)
        {
            _logger.LogWarning("gRPC {Method}: missing authorization header", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated,
                "Authorization header is required."));
        }

        var rawToken = authEntry.Value;
        var token    = rawToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? rawToken["Bearer ".Length..]
            : rawToken;

        var result = await _validator.ValidateAsync(token, context.CancellationToken)
                                     .ConfigureAwait(false);

        if (!result.IsValid)
        {
            _logger.LogWarning("gRPC {Method}: token invalid — {Error}",
                context.Method, result.ErrorMessage);
            throw new RpcException(new Status(StatusCode.Unauthenticated,
                result.ErrorMessage ?? "Token validation failed."));
        }
    }
}
