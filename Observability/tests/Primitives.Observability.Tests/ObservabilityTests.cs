using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Observability.Abstractions;
using Primitives.Observability.Extensions;
using Primitives.Observability.Log4Net.Extensions;
using Primitives.Observability.Serilog.Extensions;
using Serilog;

namespace Primitives.Observability.Tests;

// ── Core DI registration ─────────────────────────────────────────────────────

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddPrimitivesObservability_RegistersIActivitySourceProvider()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesObservability()
            .Services
            .BuildServiceProvider();

        Assert.NotNull(sp.GetService<IActivitySourceProvider>());
    }

    [Fact]
    public void AddPrimitivesObservability_DefaultOptions_ServiceNameIsUnknownService()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesObservability()
            .Services
            .BuildServiceProvider();

        var opts = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        Assert.Equal("unknown-service", opts.ServiceName);
        Assert.Equal("1.0.0",           opts.ServiceVersion);
        Assert.Equal("production",      opts.Environment);
    }

    [Fact]
    public void AddPrimitivesObservability_CustomOptions_AreApplied()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesObservability(o =>
            {
                o.ServiceName    = "order-api";
                o.ServiceVersion = "2.3.1";
                o.Environment    = "staging";
            })
            .Services
            .BuildServiceProvider();

        var opts = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        Assert.Equal("order-api", opts.ServiceName);
        Assert.Equal("2.3.1",     opts.ServiceVersion);
        Assert.Equal("staging",   opts.Environment);
    }
}

// ── IActivitySourceProvider ──────────────────────────────────────────────────

public sealed class ActivitySourceProviderTests
{
    private static IActivitySourceProvider BuildProvider(string version = "1.0.0")
    {
        return new ServiceCollection()
            .AddPrimitivesObservability(o => o.ServiceVersion = version)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IActivitySourceProvider>();
    }

    [Fact]
    public void GetSource_ReturnsNonNull()
    {
        var provider = BuildProvider();
        var source   = provider.GetSource("MyService");

        Assert.NotNull(source);
    }

    [Fact]
    public void GetSource_SameName_ReturnsSameInstance()
    {
        var provider = BuildProvider();

        var a = provider.GetSource("MyService");
        var b = provider.GetSource("MyService");

        Assert.Same(a, b);
    }

    [Fact]
    public void GetSource_DifferentNames_ReturnDifferentInstances()
    {
        var provider = BuildProvider();

        var a = provider.GetSource("ServiceA");
        var b = provider.GetSource("ServiceB");

        Assert.NotSame(a, b);
    }

    [Fact]
    public void GetSource_VersionMatchesServiceVersion()
    {
        var provider = BuildProvider(version: "3.1.0");
        var source   = provider.GetSource("MyService");

        Assert.Equal("3.1.0", source.Version);
    }

    [Fact]
    public void GetSource_NameMatchesProvidedName()
    {
        var provider = BuildProvider();
        var source   = provider.GetSource("Orders.Processing");

        Assert.Equal("Orders.Processing", source.Name);
    }
}

// ── Fluent builder chain ─────────────────────────────────────────────────────

public sealed class ObservabilityBuilderExtensionTests
{
    private static ObservabilityBuilder CreateBuilder()
        => new ServiceCollection().AddPrimitivesObservability();

    [Fact]
    public void WithTracing_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithTracing();
        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithMetrics_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithMetrics();
        Assert.Same(builder, returned);
    }

    [Fact]
    public void AddActivitySource_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.AddActivitySource("MyApp");
        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithAspNetCoreInstrumentation_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithAspNetCoreInstrumentation();
        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithHttpClientInstrumentation_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithHttpClientInstrumentation();
        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithConsoleExporter_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithConsoleExporter();
        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithOtlpExporter_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithOtlpExporter("http://localhost:4317");
        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithOtlpExporter_NullEndpoint_ReturnsSameBuilder()
    {
        var builder  = CreateBuilder();
        var returned = builder.WithOtlpExporter();
        Assert.Same(builder, returned);
    }
}

// ── Serilog integration ──────────────────────────────────────────────────────

public sealed class SerilogIntegrationTests
{
    [Fact]
    public void WithSerilog_RegistersLoggerFactory()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesObservability()
            .WithSerilog(log => log.WriteTo.Console())
            .Services
            .BuildServiceProvider();

        var factory = sp.GetRequiredService<ILoggerFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void WithSerilog_CreatesLogger()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesObservability()
            .WithSerilog(log => log.WriteTo.Console())
            .Services
            .BuildServiceProvider();

        var factory = sp.GetRequiredService<ILoggerFactory>();
        var logger  = factory.CreateLogger("Test");
        Assert.NotNull(logger);
    }

    [Fact]
    public void WithSerilog_ConfigureCallback_IsInvoked()
    {
        bool callbackInvoked = false;

        new ServiceCollection()
            .AddPrimitivesObservability()
            .WithSerilog(_ => { callbackInvoked = true; });

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void WithSerilog_ReturnsSameBuilder()
    {
        var builder  = new ServiceCollection().AddPrimitivesObservability();
        var returned = builder.WithSerilog();
        Assert.Same(builder, returned);
    }
}

// ── Log4Net integration ──────────────────────────────────────────────────────

public sealed class Log4NetIntegrationTests
{
    [Fact]
    public void WithLog4Net_RegistersLoggerFactory()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesObservability()
            .WithLog4Net("log4net.config")
            .Services
            .BuildServiceProvider();

        var factory = sp.GetRequiredService<ILoggerFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void WithLog4Net_ReturnsSameBuilder()
    {
        var builder  = new ServiceCollection().AddPrimitivesObservability();
        var returned = builder.WithLog4Net("log4net.config");
        Assert.Same(builder, returned);
    }
}
