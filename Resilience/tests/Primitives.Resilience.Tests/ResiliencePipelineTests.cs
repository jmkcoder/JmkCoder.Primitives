using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;
using Primitives.Resilience.Abstractions;
using Primitives.Resilience.Extensions;
using Primitives.Resilience.Models;

namespace Primitives.Resilience.Tests;

public sealed class ResiliencePipelineTests
{
    private static IResiliencePipelineProvider BuildProvider(Action<ResilienceOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddPrimitivesResilience(configure);
        return services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>();
    }

    // ── Provider registration ────────────────────────────────────────────────

    [Fact]
    public void AddPrimitivesResilience_RegistersIResiliencePipelineProvider()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesResilience()
            .BuildServiceProvider();

        var provider = sp.GetService<IResiliencePipelineProvider>();
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddPrimitivesResiliencePipeline_RegistersIResiliencePipelineProvider()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesResiliencePipeline("test", o => o.Retry = new RetryOptions())
            .BuildServiceProvider();

        var provider = sp.GetService<IResiliencePipelineProvider>();
        Assert.NotNull(provider);
    }

    // ── Unregistered pipeline ────────────────────────────────────────────────

    [Fact]
    public void Get_UnregisteredName_ReturnsEmptyPipeline()
    {
        var provider = BuildProvider();

        var pipeline        = provider.Get("nonexistent");
        var genericPipeline = provider.Get<string>("nonexistent");

        Assert.Same(ResiliencePipeline.Empty, pipeline);
        Assert.Same(ResiliencePipeline<string>.Empty, genericPipeline);
    }

    // ── Retry ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Retry_OnPersistentFailure_ExecutesTotalAttemptsExpectedTimes()
    {
        const int maxRetries = 2;

        var provider = BuildProvider(o =>
            o.Pipelines["retry"] = new PipelineOptions
            {
                Retry = new RetryOptions
                {
                    MaxAttempts = maxRetries,
                    BaseDelay   = TimeSpan.Zero,
                    UseJitter   = false,
                },
            });

        var pipeline   = provider.Get("retry");
        var callCount  = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.ExecuteAsync(async ct =>
            {
                callCount++;
                await Task.Yield();
                throw new InvalidOperationException("boom");
            }).AsTask());

        Assert.Equal(maxRetries + 1, callCount); // 1 initial + maxRetries
    }

    [Fact]
    public async Task Retry_OnSuccess_DoesNotRetry()
    {
        var provider = BuildProvider(o =>
            o.Pipelines["retry"] = new PipelineOptions
            {
                Retry = new RetryOptions { MaxAttempts = 3, BaseDelay = TimeSpan.Zero },
            });

        var pipeline  = provider.Get("retry");
        var callCount = 0;

        await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            return ValueTask.CompletedTask;
        });

        Assert.Equal(1, callCount);
    }

    // ── Timeout ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeout_WhenOperationExceedsDuration_ThrowsTimeoutRejectedException()
    {
        var provider = BuildProvider(o =>
            o.Pipelines["timeout"] = new PipelineOptions
            {
                Timeout = new TimeoutOptions { Timeout = TimeSpan.FromMilliseconds(50) },
            });

        var pipeline = provider.Get("timeout");

        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }).AsTask());
    }

    // ── AddPrimitivesResiliencePipeline ──────────────────────────────────────

    [Fact]
    public async Task AddPrimitivesResiliencePipeline_PipelineIsRetrievable()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesResiliencePipeline("my-pipeline", o =>
            {
                o.Retry   = new RetryOptions { MaxAttempts = 1, BaseDelay = TimeSpan.Zero };
                o.Timeout = new TimeoutOptions { Timeout = TimeSpan.FromSeconds(5) };
            })
            .BuildServiceProvider();

        var provider = sp.GetRequiredService<IResiliencePipelineProvider>();
        var pipeline = provider.Get("my-pipeline");

        // Verify the pipeline is not the Empty sentinel
        Assert.NotSame(ResiliencePipeline.Empty, pipeline);

        // Verify it executes successfully
        var ran = false;
        await pipeline.ExecuteAsync(ct =>
        {
            ran = true;
            return ValueTask.CompletedTask;
        });

        Assert.True(ran);
    }
}
