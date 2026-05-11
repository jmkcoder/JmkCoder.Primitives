using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Primitives.Caching.Abstractions;
using Primitives.Caching.Extensions;
using Primitives.Caching.Providers;

namespace Primitives.Caching.Tests;

public sealed class MemoryCacheServiceTests
{
    private static ICacheService BuildService(Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddPrimitivesCache(configure);
        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task GetOrSetAsync_MissCallsFactory_AndCaches()
    {
        var sut      = BuildService();
        var callCount = 0;

        var first  = await sut.GetOrSetAsync<string>("k1", _ => { callCount++; return Task.FromResult<string?>("hello"); });
        var second = await sut.GetOrSetAsync<string>("k1", _ => { callCount++; return Task.FromResult<string?>("world"); });

        Assert.Equal("hello", first);
        Assert.Equal("hello", second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesEntry()
    {
        var sut = BuildService();
        await sut.SetAsync("k2", "value");
        await sut.InvalidateAsync("k2");

        var result = await sut.GetAsync<string>("k2");
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateByTagAsync_RemovesTaggedEntries()
    {
        var sut = BuildService();
        var opts = new CacheEntryOptions { Tags = ["group-a"] };
        await sut.SetAsync("k3", "one",   opts);
        await sut.SetAsync("k4", "two",   opts);
        await sut.SetAsync("k5", "three"); // untagged

        await sut.InvalidateByTagAsync("group-a");

        Assert.Null(await sut.GetAsync<string>("k3"));
        Assert.Null(await sut.GetAsync<string>("k4"));
        Assert.Equal("three", await sut.GetAsync<string>("k5"));
    }

    [Fact]
    public async Task KeyPrefix_IsApplied()
    {
        var sut = BuildService(o => o.KeyPrefix = "ns");
        await sut.SetAsync("item", 42);

        var result = await sut.GetAsync<int>("item");
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenMissing()
    {
        var sut = BuildService();
        var result = await sut.GetAsync<string>("missing");
        Assert.Null(result);
    }
}
