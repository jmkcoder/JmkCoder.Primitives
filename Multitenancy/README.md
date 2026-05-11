# Primitives.Multitenancy

Tenant resolution, per-tenant configuration, and data-isolation strategies that slot into any existing ASP.NET Core application.

## Package

| Package | Description |
|---------|-------------|
| `Primitives.Multitenancy` | Core interfaces, built-in resolvers, in-memory tenant store, and ASP.NET Core middleware |

## Quick start

```bash
dotnet add package Primitives.Multitenancy
```

```csharp
// Program.cs
builder.Services
    .AddPrimitivesMultitenancy(o =>
    {
        o.RequireTenant = true; // reject requests that don't resolve to a tenant
    })
    .WithHeaderResolver()     // reads X-Tenant-Id header
    .WithInMemoryTenants(t =>
    {
        t.Add(new Tenant { Id = "acme",  Name = "Acme Corp" });
        t.Add(new Tenant { Id = "bigco", Name = "Big Co" });
    });

// Middleware pipeline
app.UsePrimitivesMultitenancy(); // after UseRouting, after UseAuthentication
```

```csharp
// Inject and use
public class OrderService(ITenantAccessor tenant, OrderRepository repo)
{
    public Task<List<Order>> GetOrdersAsync(CancellationToken ct) =>
        repo.GetOrdersForTenantAsync(tenant.Tenant!.Id, ct);
}
```

## Resolver strategies

| Resolver | Source | Default key |
|----------|--------|-------------|
| Header | HTTP request header | `X-Tenant-Id` |
| Host | Subdomain or host-map | Leftmost subdomain |
| Route value | Route parameter | `tenantId` |
| Claim | Authenticated user claim | `tenant_id` |
| Query string | URL parameter | `tenantId` |

Multiple strategies can be combined — the composite resolver tries each in registration order:

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithClaimResolver("tenant_id")   // 1st — authenticated user
    .WithHostResolver()               // 2nd — subdomain fallback
    .WithHeaderResolver();            // 3rd — explicit header override
```

## Data isolation pattern

```csharp
public class ProductRepository(AppDbContext db, ITenantAccessor tenant)
{
    public Task<List<Product>> GetAsync(CancellationToken ct) =>
        db.Products
          .Where(p => p.TenantId == tenant.Tenant!.Id)
          .ToListAsync(ct);
}
```

## Per-tenant metadata

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithInMemoryTenants(t => t.Add(new Tenant
    {
        Id   = "acme",
        Name = "Acme Corp",
        Claims = new Dictionary<string, string>
        {
            ["plan"]               = "enterprise",
            ["connection-string"]  = "Server=acme-db;…",
        },
    }));

// At runtime
var plan = tenantAccessor.Tenant!.Claims["plan"];
```

## Custom tenant store

```csharp
public sealed class DbTenantStore(AppDbContext db) : ITenantStore
{
    public async Task<Tenant?> FindByIdentifierAsync(string id, CancellationToken ct) =>
        (await db.Tenants.FindAsync([id], ct))
            ?.ToTenant(); // map to Primitives.Tenant
}

builder.Services
    .AddPrimitivesMultitenancy()
    .AddTenantStore<DbTenantStore>();
```

## License

MIT
