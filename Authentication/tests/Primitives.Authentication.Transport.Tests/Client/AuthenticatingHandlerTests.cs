using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Client.Http;
using Primitives.Authentication.Strategies.TokenIssuance;
using System.Net;

namespace Primitives.Authentication.Transport.Tests.Client;

public sealed class AuthenticatingHandlerTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static (AuthenticatingHandler handler, HttpClient client, TestInnerHandler inner) Build(
        ITokenIssuanceService tokenService,
        string strategy = "Test")
    {
        var opts    = new AuthenticatingHandlerOptions { StrategyName = strategy };
        var logger  = NullLogger<AuthenticatingHandler>.Instance;
        var inner   = new TestInnerHandler();
        var handler = new AuthenticatingHandler(tokenService, opts, logger) { InnerHandler = inner };
        var client  = new HttpClient(handler);
        return (handler, client, inner);
    }

    private static AuthenticationResult OkResult(string token = "tok", string? refresh = "rt1") =>
        AuthenticationResult.Success(token, "Bearer", DateTimeOffset.UtcNow.AddMinutes(15),
            subject: "user", refreshToken: refresh);

    private static AuthenticationResult FailResult() =>
        AuthenticationResult.Failure("bad creds");

    // ── happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_AttachesAuthorizationHeader_OnSuccess()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(OkResult("my-jwt"));

        var (_, client, inner) = Build(svc);
        inner.Respond(HttpStatusCode.OK);

        await client.GetAsync("http://api.test/resource");

        Assert.Equal("Bearer my-jwt", inner.LastRequest!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task SendAsync_DoesNotAttachHeader_WhenAuthFails()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(FailResult());

        var (_, client, inner) = Build(svc);
        inner.Respond(HttpStatusCode.OK);

        await client.GetAsync("http://api.test/resource");

        Assert.Null(inner.LastRequest!.Headers.Authorization);
    }

    // ── 401 retry ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Retries_With_RefreshedToken_On401()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        // Initial auth: returns token + refresh token
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(OkResult("old-tok", "rt-old"));
        // Refresh returns a new JWT
        svc.RefreshAsync("rt-old", Arg.Any<CancellationToken>())
           .Returns(OkResult("new-tok", "rt-new"));

        var (_, client, inner) = Build(svc);
        inner.RespondSequence(HttpStatusCode.Unauthorized, HttpStatusCode.OK);

        var response = await client.GetAsync("http://api.test/secure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // RefreshAsync was called exactly once
        await svc.Received(1).RefreshAsync("rt-old", Arg.Any<CancellationToken>());
        // Second request carried the new token
        Assert.Equal("Bearer new-tok", inner.LastRequest!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task SendAsync_FallsBack_ToReauth_WhenRefreshFails()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(OkResult("tok1", "rt1"),
                    OkResult("tok2", "rt2"));   // second call after failed refresh
        svc.RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(FailResult());

        var (_, client, inner) = Build(svc);
        inner.RespondSequence(HttpStatusCode.Unauthorized, HttpStatusCode.OK);

        var response = await client.GetAsync("http://api.test/secure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Fell back to full re-auth
        await svc.Received(2).AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal("Bearer tok2", inner.LastRequest!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task SendAsync_Returns401_WhenBothRefreshAndReauthFail()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(OkResult("tok1", null),    // first (no refresh token → goes straight to reauth)
                    FailResult());              // reauth also fails
        svc.RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(FailResult());

        var (_, client, inner) = Build(svc);
        inner.Respond(HttpStatusCode.Unauthorized);

        var response = await client.GetAsync("http://api.test/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_Returns401_WithoutRetry_ForStreamingBody()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(OkResult());

        var (_, client, inner) = Build(svc);
        inner.Respond(HttpStatusCode.Unauthorized);

        // StreamContent is a non-buffered, non-retryable body
        var request = new HttpRequestMessage(HttpMethod.Post, "http://api.test/upload")
        {
            Content = new StreamContent(new MemoryStream([1, 2, 3])),
        };
        var response = await client.SendAsync(request);

        // Should not attempt any refresh
        await svc.DidNotReceive().RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── inner handler helper ─────────────────────────────────────────────────

    private sealed class TestInnerHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _responses = new();
        public HttpRequestMessage? LastRequest { get; private set; }

        public void Respond(HttpStatusCode code) => _responses.Enqueue(code);

        public void RespondSequence(params HttpStatusCode[] codes)
        {
            foreach (var c in codes) _responses.Enqueue(c);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken  cancellationToken)
        {
            LastRequest = request;
            var code = _responses.Count > 0 ? _responses.Dequeue() : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(code));
        }
    }
}
