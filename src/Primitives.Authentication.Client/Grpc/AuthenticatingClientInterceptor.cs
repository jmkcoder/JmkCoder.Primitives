using Grpc.Core;
using Grpc.Core.Interceptors;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Client.Grpc;

/// <summary>
/// gRPC client interceptor that injects a Bearer JWT into every outgoing call's
/// metadata.  Supports all four call types (unary, client-streaming,
/// server-streaming, bidirectional).
/// </summary>
/// <remarks>
/// Prefer <see cref="PrimitivesGrpcCredentials.Create"/> (CallCredentials) for
/// TLS channels because it participates in the gRPC-core credential refresh
/// mechanism.  Use this interceptor for insecure / development channels.
/// </remarks>
public sealed class AuthenticatingClientInterceptor : Interceptor
{
    private readonly ITokenIssuanceService _tokenService;
    private readonly string               _strategyName;

    internal AuthenticatingClientInterceptor(
        ITokenIssuanceService tokenService,
        string               strategyName)
    {
        _tokenService = tokenService;
        _strategyName = strategyName;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest                                     request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        // Build the enriched context asynchronously; use a lazy-capture pattern
        // so status/trailers are read from the inner call once it is available.
        AsyncUnaryCall<TResponse>? inner = null;

        async Task<TResponse> ResponseAsync()
        {
            inner = continuation(request, await WithTokenAsync(context).ConfigureAwait(false));
            return await inner.ResponseAsync.ConfigureAwait(false);
        }

        var responseTask = ResponseAsync();

        return new AsyncUnaryCall<TResponse>(
            responseTask,
            responseTask.ContinueWith(_ => inner?.ResponseHeadersAsync ?? Task.FromResult(new Metadata()),
                TaskContinuationOptions.ExecuteSynchronously).Unwrap(),
            () => inner?.GetStatus() ?? Status.DefaultSuccess,
            () => inner?.GetTrailers() ?? new Metadata(),
            () => inner?.Dispose());
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest                                      request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        => continuation(request, WithTokenAsync(context).GetAwaiter().GetResult());

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse>              context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse>  continuation)
    {
        AsyncClientStreamingCall<TRequest, TResponse>? inner = null;

        async Task<TResponse> ResponseAsync()
        {
            inner = continuation(await WithTokenAsync(context).ConfigureAwait(false));
            return await inner.ResponseAsync.ConfigureAwait(false);
        }

        var responseTask = ResponseAsync();
        return new AsyncClientStreamingCall<TRequest, TResponse>(
            new BarrierClientStream<TRequest>(responseTask, () => inner?.RequestStream!),
            responseTask,
            responseTask.ContinueWith(_ => inner?.ResponseHeadersAsync ?? Task.FromResult(new Metadata()),
                TaskContinuationOptions.ExecuteSynchronously).Unwrap(),
            () => inner?.GetStatus() ?? Status.DefaultSuccess,
            () => inner?.GetTrailers() ?? new Metadata(),
            () => inner?.Dispose());
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest                                            request,
        ClientInterceptorContext<TRequest, TResponse>       context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        AsyncServerStreamingCall<TResponse>? inner = null;

        async Task EnqueueAsync()
        {
            inner = continuation(request, await WithTokenAsync(context).ConfigureAwait(false));
        }

        var enqueue = EnqueueAsync();
        return new AsyncServerStreamingCall<TResponse>(
            new BarrierServerStream<TResponse>(enqueue, () => inner?.ResponseStream!),
            enqueue.ContinueWith(_ => inner?.ResponseHeadersAsync ?? Task.FromResult(new Metadata()),
                TaskContinuationOptions.ExecuteSynchronously).Unwrap(),
            () => inner?.GetStatus() ?? Status.DefaultSuccess,
            () => inner?.GetTrailers() ?? new Metadata(),
            () => inner?.Dispose());
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse>             context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        AsyncDuplexStreamingCall<TRequest, TResponse>? inner = null;

        async Task EnqueueAsync()
        {
            inner = continuation(await WithTokenAsync(context).ConfigureAwait(false));
        }

        var enqueue = EnqueueAsync();
        return new AsyncDuplexStreamingCall<TRequest, TResponse>(
            new BarrierClientStream<TRequest>(enqueue, () => inner?.RequestStream!),
            new BarrierServerStream<TResponse>(enqueue, () => inner?.ResponseStream!),
            enqueue.ContinueWith(_ => inner?.ResponseHeadersAsync ?? Task.FromResult(new Metadata()),
                TaskContinuationOptions.ExecuteSynchronously).Unwrap(),
            () => inner?.GetStatus() ?? Status.DefaultSuccess,
            () => inner?.GetTrailers() ?? new Metadata(),
            () => inner?.Dispose());
    }

    // -------------------------------------------------------------------------

    private async Task<ClientInterceptorContext<TRequest, TResponse>> WithTokenAsync<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest  : class
        where TResponse : class
    {
        var result  = await _tokenService.AuthenticateAsync(_strategyName).ConfigureAwait(false);
        var headers = context.Options.Headers ?? new Metadata();

        if (result.IsSuccess && result.AccessToken is { Length: > 0 } token)
            headers.Add("Authorization", $"Bearer {token}");

        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, context.Options.WithHeaders(headers));
    }

    // -------------------------------------------------------------------------
    // Barrier wrappers: delay stream access until the inner call is created.

    private sealed class BarrierClientStream<T>(Task gate, Func<IClientStreamWriter<T>> inner)
        : IClientStreamWriter<T> where T : class
    {
        public WriteOptions? WriteOptions
        {
            get => inner().WriteOptions;
            set => inner().WriteOptions = value;
        }

        public async Task WriteAsync(T message)
        {
            await gate.ConfigureAwait(false);
            await inner().WriteAsync(message).ConfigureAwait(false);
        }

        public async Task CompleteAsync()
        {
            await gate.ConfigureAwait(false);
            await inner().CompleteAsync().ConfigureAwait(false);
        }
    }

    private sealed class BarrierServerStream<T>(Task gate, Func<IAsyncStreamReader<T>> inner)
        : IAsyncStreamReader<T> where T : class
    {
        public T Current => inner().Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            await gate.ConfigureAwait(false);
            return await inner().MoveNext(cancellationToken).ConfigureAwait(false);
        }
    }
}
