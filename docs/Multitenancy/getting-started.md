---
layout: default
library: multitenancy
title: Installation
description: Add Primitives.Multitenancy to an ASP.NET Core 8 project and register the middleware.
permalink: /multitenancy/getting-started/
---

## Requirements

- .NET 8 or later
- ASP.NET Core (included in `Microsoft.AspNetCore.App` shared framework)

## Install

```bash
dotnet add package Primitives.Multitenancy
```

## Minimal setup

```csharp
// Program.cs
builder.Services
    .AddPrimitivesMultitenancy()
    .WithHeaderResolver();          // reads X-Tenant-Id header

app.UsePrimitivesMultitenancy();    // before MapControllers / MapEndpoints
```

With `RequireTenant` left at its default (`false`), requests that cannot be resolved to a tenant
pass through normally — `ITenantAccessor.Tenant` returns `null`.

## Register tenants

### In-memory (development / small fixed sets)

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithHeaderResolver()
    .WithInMemoryTenants(t =>
    {
        t.Add(new Tenant { Id = "acme",  Name = "Acme Corp" });
        t.Add(new Tenant { Id = "bigco", Name = "Big Co" });
    });
```

### Database-backed

```csharp
public sealed class DbTenantStore(AppDbContext db) : ITenantStore
{
    public async Task<Tenant?> FindByIdentifierAsync(string id, CancellationToken ct) =>
        await db.Tenants
                .Where(t => t.Id == id)
                .Select(t => new Tenant { Id = t.Id, Name = t.Name })
                .FirstOrDefaultAsync(ct);
}

builder.Services
    .AddPrimitivesMultitenancy()
    .WithHeaderResolver()
    .AddTenantStore<DbTenantStore>();
```

## Enforce tenant resolution

Set `RequireTenant = true` to reject requests that cannot be resolved to a known tenant before
they reach application code:

```csharp
builder.Services.AddPrimitivesMultitenancy(o =>
{
    o.RequireTenant            = true;
    o.TenantNotFoundStatusCode = 400;  // default
});
```

Requests with no resolvable tenant receive `400 Bad Request` and the rest of the pipeline is
not executed.

## Middleware placement

```csharp
app.UseRouting();           // required if using WithRouteValueResolver
app.UseAuthentication();    // required if using WithClaimResolver
app.UsePrimitivesMultitenancy();
app.UseAuthorization();
app.MapControllers();
```

> **Important:** Place `UsePrimitivesMultitenancy` after `UseRouting` and `UseAuthentication`
> so that route values and user claims are populated when the resolver runs.

## Inject the current tenant

```csharp
// Scoped service — safe in controllers, minimal API handlers, etc.
public class InvoiceController(ITenantAccessor tenant) : ControllerBase
{
    [HttpGet]
    public IActionResult GetTenantInfo() =>
        Ok(new { tenant.Tenant?.Id, tenant.Tenant?.Name });
}

// Singleton service — safe because ITenantAccessor reads from IHttpContextAccessor
public class AuditService(ITenantAccessor tenant, ILogger<AuditService> logger)
{
    public void LogAction(string action) =>
        logger.LogInformation("Tenant={TenantId} Action={Action}", tenant.Tenant?.Id, action);
}
```

## Next steps

- [Resolvers]({{ '/multitenancy/resolvers/' | relative_url }}) — host, header, route value, claim, query string
- [Data Isolation]({{ '/multitenancy/data-isolation/' | relative_url }}) — per-tenant databases and row-level filtering
- [Configuration Reference]({{ '/multitenancy/reference/' | relative_url }}) — all options tables
