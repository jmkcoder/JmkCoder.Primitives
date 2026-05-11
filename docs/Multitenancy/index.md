---
layout: default
library: multitenancy
permalink: /multitenancy/
---

<div class="bd-hero">
  <h1>Primitives.Multitenancy</h1>
  <p class="lead">
    Tenant resolution, per-tenant configuration, and data-isolation strategies
    that slot into any existing ASP.NET Core application — one middleware registration,
    five resolver strategies, one interface to inject.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Multitenancy
  </div>
</div>

## The problem it solves

Building a multi-tenant SaaS application in ASP.NET Core requires solving the same problems
every time: figuring out which tenant owns the request, loading tenant configuration, and keeping
tenant data isolated. Most teams end up with ad-hoc middleware, scattered tenant ID checks, and
tight coupling to a specific resolution strategy.

`Primitives.Multitenancy` gives you:

- **One interface** — `ITenantAccessor` — injectable anywhere; returns `null` outside tenanted requests.
- **Five built-in resolvers** — host, header, route value, claim, query string. Combine them in priority order.
- **Middleware** — `UsePrimitivesMultitenancy()` resolves the tenant on every request; your code never touches `HttpContext.Request` directly.
- **Pluggable store** — ship with in-memory tenants; swap in a `DbTenantStore` when you're ready.
- **Zero framework lock-in** — register, resolve, access. No base classes, no required attributes.

## Quick start

```csharp
// Program.cs
builder.Services
    .AddPrimitivesMultitenancy(o => o.RequireTenant = true)
    .WithHeaderResolver()        // reads X-Tenant-Id
    .WithInMemoryTenants(t =>
    {
        t.Add(new Tenant { Id = "acme",  Name = "Acme Corp" });
        t.Add(new Tenant { Id = "bigco", Name = "Big Co" });
    });

app.UseAuthentication();
app.UsePrimitivesMultitenancy(); // ← place here
app.UseAuthorization();
```

```csharp
// Inject and use
public class OrderService(ITenantAccessor tenant, OrderRepository repo)
{
    public Task<List<Order>> GetAsync(CancellationToken ct) =>
        repo.GetForTenantAsync(tenant.Tenant!.Id, ct);
}
```

## Package

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Multitenancy</div>
    <p>Core <code>ITenantAccessor</code>, <code>ITenantResolver</code>, <code>ITenantStore</code>, five built-in resolvers, in-memory store, and ASP.NET Core middleware.</p>
    <div class="install-cmd">dotnet add package Primitives.Multitenancy</div>
  </div>
</div>

## How it works

```
HTTP Request
    │
    ▼
TenantResolutionMiddleware
    ├─ ITenantResolver.ResolveAsync()  → "acme"   (identifier string)
    ├─ ITenantStore.FindByIdentifierAsync("acme")  → Tenant { Id="acme", … }
    └─ HttpContext.Items[key] = tenant
    │
    ▼
Your endpoint / service
    └─ ITenantAccessor.Tenant  → Tenant { Id="acme", … }
```

`ITenantAccessor` reads from `HttpContext.Items` via `IHttpContextAccessor` — scoped to the current request, zero allocations after middleware runs, safe to inject into singleton services.
