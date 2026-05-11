using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Tests.TokenIssuance;

public class InMemoryRefreshTokenStoreTests
{
    private static JwtOptions DefaultOpts() => new()
    {
        Issuer             = "test",
        Audience           = "test",
        SigningKey          = "a-very-long-signing-key-for-testing-purposes!",
        RefreshTokenLifetime = TimeSpan.FromDays(7),
    };

    private static (InMemoryRefreshTokenStore Store, FakeTimeProvider Time) Create(DateTimeOffset? now = null)
    {
        var time  = new FakeTimeProvider(now ?? DateTimeOffset.UtcNow);
        var store = new InMemoryRefreshTokenStore(Options.Create(DefaultOpts()), time);
        return (store, time);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsNonEmptyToken()
    {
        var (store, _) = Create();
        var token = await store.GenerateAsync("alice");
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task GenerateAsync_TwoCalls_ReturnDifferentTokens()
    {
        var (store, _) = Create();
        var t1 = await store.GenerateAsync("alice");
        var t2 = await store.GenerateAsync("alice");
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ReturnsFailure_ForUnknownToken()
    {
        var (store, _) = Create();
        var result = await store.ValidateAndRotateAsync("unknown-token");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_RotatesToken_ReturnsNewTokenAndSubject()
    {
        var (store, _) = Create();
        var original  = await store.GenerateAsync("bob");

        var result = await store.ValidateAndRotateAsync(original);

        Assert.True(result.IsValid);
        Assert.NotNull(result.NewToken);
        Assert.NotEqual(original, result.NewToken);
        Assert.Equal("bob", result.Subject);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_OldToken_RejectedAfterRotation()
    {
        var (store, _) = Create();
        var original = await store.GenerateAsync("bob");
        await store.ValidateAndRotateAsync(original);

        // Trying to use the original (now rotated) token should fail
        var reuse = await store.ValidateAndRotateAsync(original);
        Assert.False(reuse.IsValid);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ExpiredToken_Rejected()
    {
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (store, time) = Create(baseTime);

        var token = await store.GenerateAsync("carol");

        // Advance time past expiry
        time.Advance(TimeSpan.FromDays(8));

        var result = await store.ValidateAndRotateAsync(token);
        Assert.False(result.IsValid);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RevokeAsync_RevokesToken()
    {
        var (store, _) = Create();
        var token = await store.GenerateAsync("dave");

        await store.RevokeAsync(token);

        var result = await store.ValidateAndRotateAsync(token);
        Assert.False(result.IsValid);
        Assert.Contains("revoked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
