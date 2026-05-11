using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Primitives.Multitenancy.Abstractions;
using Primitives.Multitenancy.Extensions;
using Primitives.Multitenancy.Internal;
using Primitives.Multitenancy.Middleware;
using Primitives.Multitenancy.Models;
using Primitives.Multitenancy.Resolvers;

namespace Primitives.Multitenancy.Tests;

// ── DI registration ──────────────────────────────────────────────────────────

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddPrimitivesMultitenancy_RegistersRequiredServices()
    {
        var sp = new ServiceCollection()
            .AddPrimitivesMultitenancy()
            .Services
            .BuildServiceProvider();

        Assert.NotNull(sp.GetService<ITenantResolver>());
        Assert.NotNull(sp.GetService<ITenantStore>());

        // ITenantAccessor is scoped
        using var scope = sp.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<ITenantAccessor>());
    }
}

// ── Resolver tests ───────────────────────────────────────────────────────────

public sealed class HeaderTenantResolverTests
{
    [Fact]
    public async Task ResolvesFromDefaultHeader()
    {
        var resolver = new HeaderTenantResolver();
        var context  = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("acme", result);
    }

    [Fact]
    public async Task ReturnsNull_WhenHeaderAbsent()
    {
        var resolver = new HeaderTenantResolver();
        var result   = await resolver.ResolveAsync(new DefaultHttpContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolvesFromCustomHeader()
    {
        var resolver = new HeaderTenantResolver(new HeaderResolverOptions { HeaderName = "X-Account" });
        var context  = new DefaultHttpContext();
        context.Request.Headers["X-Account"] = "bigco";

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("bigco", result);
    }
}

public sealed class HostTenantResolverTests
{
    [Fact]
    public async Task ExtractsSubdomain()
    {
        var resolver = new HostTenantResolver();
        var context  = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com");

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("acme", result);
    }

    [Fact]
    public async Task ReturnsNull_ForSinglePartHost()
    {
        var resolver = new HostTenantResolver();
        var context  = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost");

        var result = await resolver.ResolveAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task HostMap_TakesPrecedenceOverSubdomain()
    {
        var resolver = new HostTenantResolver(new HostResolverOptions
        {
            HostMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["acme.com"] = "acme-mapped",
            },
        });
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.com");

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("acme-mapped", result);
    }
}

public sealed class QueryStringTenantResolverTests
{
    [Fact]
    public async Task ResolvesFromQueryString()
    {
        var resolver = new QueryStringTenantResolver();
        var context  = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?tenantId=acme");

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("acme", result);
    }

    [Fact]
    public async Task ReturnsNull_WhenParameterAbsent()
    {
        var resolver = new QueryStringTenantResolver();
        var result   = await resolver.ResolveAsync(new DefaultHttpContext());

        Assert.Null(result);
    }
}

public sealed class ClaimTenantResolverTests
{
    [Fact]
    public async Task ResolvesFromClaim()
    {
        var resolver = new ClaimTenantResolver();
        var context  = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("tenant_id", "bigco") }, "test"));

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("bigco", result);
    }

    [Fact]
    public async Task ReturnsNull_WhenClaimAbsent()
    {
        var resolver = new ClaimTenantResolver();
        var context  = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await resolver.ResolveAsync(context);

        Assert.Null(result);
    }
}

public sealed class RouteValueTenantResolverTests
{
    [Fact]
    public async Task ResolvesFromRouteValue()
    {
        var resolver = new RouteValueTenantResolver();
        var context  = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "acme";

        var result = await resolver.ResolveAsync(context);

        Assert.Equal("acme", result);
    }
}

public sealed class CompositeTenantResolverTests
{
    [Fact]
    public async Task ReturnsFirstNonNullResult()
    {
        // Header returns null; host returns "acme"
        var headerResolver = new HeaderTenantResolver();
        var hostResolver   = new HostTenantResolver();

        var composite = new CompositeTenantResolver(new ITenantResolverStrategy[]
            { headerResolver, hostResolver });

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com");

        var result = await composite.ResolveAsync(context);

        Assert.Equal("acme", result);
    }

    [Fact]
    public async Task ReturnsNull_WhenAllStrategiesFail()
    {
        var composite = new CompositeTenantResolver(new ITenantResolverStrategy[]
        {
            new HeaderTenantResolver(),
            new QueryStringTenantResolver(),
        });

        var result = await composite.ResolveAsync(new DefaultHttpContext());

        Assert.Null(result);
    }
}

// ── Store tests ──────────────────────────────────────────────────────────────

public sealed class InMemoryTenantStoreTests
{
    private static InMemoryTenantStore BuildStore(params Tenant[] tenants)
    {
        var options = Options.Create(new MultitenancyOptions
        {
            Tenants = tenants.ToList(),
        });
        return new InMemoryTenantStore(options);
    }

    [Fact]
    public async Task FindByIdentifier_ReturnsTenant()
    {
        var store  = BuildStore(new Tenant { Id = "acme", Name = "Acme Corp" });
        var tenant = await store.FindByIdentifierAsync("acme");

        Assert.NotNull(tenant);
        Assert.Equal("Acme Corp", tenant.Name);
    }

    [Fact]
    public async Task FindByIdentifier_CaseInsensitive()
    {
        var store  = BuildStore(new Tenant { Id = "ACME" });
        var tenant = await store.FindByIdentifierAsync("acme");

        Assert.NotNull(tenant);
    }

    [Fact]
    public async Task FindByIdentifier_ReturnsNull_WhenNotFound()
    {
        var store  = BuildStore(new Tenant { Id = "acme" });
        var tenant = await store.FindByIdentifierAsync("nobody");

        Assert.Null(tenant);
    }
}

// ── Middleware tests ─────────────────────────────────────────────────────────

public sealed class TenantResolutionMiddlewareTests
{
    private static TenantResolutionMiddleware BuildMiddleware(
        RequestDelegate next,
        ITenantResolver? resolver = null,
        ITenantStore? store = null,
        MultitenancyOptions? options = null)
    {
        options ??= new MultitenancyOptions();
        store   ??= new InMemoryTenantStore(Options.Create(options));

        return new TenantResolutionMiddleware(
            next,
            resolver ?? new HeaderTenantResolver(),
            store,
            Options.Create(options),
            NullLogger<TenantResolutionMiddleware>.Instance);
    }

    [Fact]
    public async Task SetsCurrentTenant_WhenResolved()
    {
        var tenant  = new Tenant { Id = "acme", Name = "Acme Corp" };
        var options = new MultitenancyOptions { Tenants = { tenant } };
        var store   = new InMemoryTenantStore(Options.Create(options));

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";

        var middleware = BuildMiddleware(_ => Task.CompletedTask, store: store, options: options);
        await middleware.InvokeAsync(context);

        Assert.Same(tenant, context.Items[TenantResolutionMiddleware.TenantItemKey]);
    }

    [Fact]
    public async Task CallsNext_WhenTenantResolved()
    {
        var tenant  = new Tenant { Id = "acme" };
        var options = new MultitenancyOptions { Tenants = { tenant } };

        var context  = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";

        bool nextCalled = false;
        var middleware  = BuildMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            store:   new InMemoryTenantStore(Options.Create(options)),
            options: options);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task RequireTenant_Returns400_WhenNotResolved()
    {
        var options = new MultitenancyOptions
        {
            RequireTenant           = true,
            TenantNotFoundStatusCode = 400,
        };

        var context    = new DefaultHttpContext();
        bool nextCalled = false;
        var middleware  = BuildMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            options: options);

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task RequireTenant_CallsNext_WhenTenantResolved()
    {
        var tenant  = new Tenant { Id = "acme" };
        var options = new MultitenancyOptions
        {
            RequireTenant = true,
            Tenants       = { tenant },
        };

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "acme";

        bool nextCalled = false;
        var middleware  = BuildMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            store:   new InMemoryTenantStore(Options.Create(options)),
            options: options);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
