using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Client.MessageQueue;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Transport.Tests.Client;

public sealed class MessageTokenAttacherTests
{
    private static ITokenIssuanceService OkService(string token = "jwt-abc") =>
        NSubstitute.Substitute.For<ITokenIssuanceService>()
            .WithResult(AuthenticationResult.Success(token));

    private static ITokenIssuanceService FailService() =>
        NSubstitute.Substitute.For<ITokenIssuanceService>()
            .WithResult(AuthenticationResult.Failure("credentials rejected"));

    [Fact]
    public async Task AttachAsync_WritesAuthorizationHeader_OnSuccess()
    {
        var svc      = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync("OIDC", Arg.Any<CancellationToken>())
           .Returns(AuthenticationResult.Success("jwt-abc"));

        var attacher = new MessageTokenAttacher(svc, NullLogger<MessageTokenAttacher>.Instance);
        var headers  = new Dictionary<string, string>();

        var ok = await attacher.AttachAsync(headers, "OIDC");

        Assert.True(ok);
        Assert.Equal("Bearer jwt-abc", headers["Authorization"]);
    }

    [Fact]
    public async Task AttachAsync_ReturnsFalse_WhenAuthFails()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync("OIDC", Arg.Any<CancellationToken>())
           .Returns(AuthenticationResult.Failure("bad creds"));

        var attacher = new MessageTokenAttacher(svc, NullLogger<MessageTokenAttacher>.Instance);
        var headers  = new Dictionary<string, string>();

        var ok = await attacher.AttachAsync(headers, "OIDC");

        Assert.False(ok);
        Assert.DoesNotContain("Authorization", headers.Keys);
    }

    [Fact]
    public async Task AttachAsync_PassesCancellationToken_ToService()
    {
        var svc = Substitute.For<ITokenIssuanceService>();
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(AuthenticationResult.Success("tok"));

        var attacher = new MessageTokenAttacher(svc, NullLogger<MessageTokenAttacher>.Instance);
        using var cts = new CancellationTokenSource();

        await attacher.AttachAsync(new Dictionary<string, string>(), "X", cts.Token);

        await svc.Received(1).AuthenticateAsync("X", cts.Token);
    }

    [Fact]
    public async Task AttachAsync_Throws_WhenHeadersIsNull()
    {
        var svc      = Substitute.For<ITokenIssuanceService>();
        var attacher = new MessageTokenAttacher(svc, NullLogger<MessageTokenAttacher>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            attacher.AttachAsync(null!, "OIDC"));
    }

    [Fact]
    public async Task AttachAsync_Throws_WhenStrategyNameIsEmpty()
    {
        var svc      = Substitute.For<ITokenIssuanceService>();
        var attacher = new MessageTokenAttacher(svc, NullLogger<MessageTokenAttacher>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            attacher.AttachAsync(new Dictionary<string, string>(), ""));
    }
}

// ── test helpers ─────────────────────────────────────────────────────────────

file static class TokenServiceExtensions
{
    internal static ITokenIssuanceService WithResult(
        this ITokenIssuanceService svc,
        AuthenticationResult       result)
    {
        svc.AuthenticateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(result);
        return svc;
    }
}
