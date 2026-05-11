using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Primitives.Authentication.AspNetCore.Grpc;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Transport.Tests.AspNetCore;

public sealed class AuthenticationServerInterceptorTests
{
    private static AuthenticationServerInterceptor Build(IJwtTokenValidator validator) =>
        new(validator, NullLogger<AuthenticationServerInterceptor>.Instance);

    // ── happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UnaryHandler_Passes_ValidBearerToken()
    {
        var validator = Substitute.For<IJwtTokenValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .Returns(JwtValidationResult.Success(TestPrincipal()));

        var interceptor = Build(validator);
        var context     = BuildContext("Bearer good-token");
        var called      = false;

        await interceptor.UnaryServerHandler(
            new object(), context,
            (_, _) => { called = true; return Task.FromResult(new object()); });

        Assert.True(called);
    }

    [Fact]
    public async Task UnaryHandler_Rejects_MissingAuthHeader()
    {
        var validator   = Substitute.For<IJwtTokenValidator>();
        var interceptor = Build(validator);
        var context     = BuildContext(authHeader: null);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                new object(), context,
                (_, _) => Task.FromResult(new object())));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task UnaryHandler_Rejects_InvalidToken()
    {
        var validator = Substitute.For<IJwtTokenValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .Returns(JwtValidationResult.Failure("token expired"));

        var interceptor = Build(validator);
        var context     = BuildContext("Bearer bad-token");

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                new object(), context,
                (_, _) => Task.FromResult(new object())));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task UnaryHandler_AcceptsToken_WithoutBearerPrefix()
    {
        // Some gRPC clients send just the raw token without "Bearer "
        var validator = Substitute.For<IJwtTokenValidator>();
        validator.ValidateAsync("raw-token", Arg.Any<CancellationToken>())
                 .Returns(JwtValidationResult.Success(TestPrincipal()));

        var interceptor = Build(validator);
        var context     = BuildContext("raw-token");   // no "Bearer " prefix
        var called      = false;

        await interceptor.UnaryServerHandler(
            new object(), context,
            (_, _) => { called = true; return Task.FromResult(new object()); });

        Assert.True(called);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static System.Security.Claims.ClaimsPrincipal TestPrincipal() =>
        new(new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("sub", "test-user")], "jwt"));

    private static TestServerCallContext BuildContext(string? authHeader)
    {
        var metadata = new Metadata();
        if (authHeader is not null)
            metadata.Add("authorization", authHeader);
        return new TestServerCallContext(metadata);
    }

    // ── gRPC ServerCallContext stub ───────────────────────────────────────────
    // ServerCallContext is abstract; we provide a minimal concrete stub.

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _requestHeaders;

        public TestServerCallContext(Metadata requestHeaders)
            => _requestHeaders = requestHeaders;

        protected override string MethodCore          => "/test.Service/Method";
        protected override string HostCore            => "localhost";
        protected override string PeerCore            => "ipv4:127.0.0.1:12345";
        protected override DateTime DeadlineCore      => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore =>
            new("ssl", []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
            => Task.CompletedTask;
    }
}
