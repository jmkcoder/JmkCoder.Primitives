---
layout: default
library: multitenancy
title: Data Isolation
description: Row-level filtering, per-tenant connection strings, and database-per-tenant patterns.
permalink: /multitenancy/data-isolation/
---

## Pattern 1 — Row-level filtering (shared database)

The simplest approach: all tenants share one database, rows are tagged with a `TenantId` column, and
every query filters on that column.

```csharp
// Entity
public class Order
{
    public Guid   Id       { get; set; }
    public string TenantId { get; set; } = default!;
    // …
}

// Repository
public class OrderRepository(AppDbContext db, ITenantAccessor tenant)
{
    public Task<List<Order>> GetAsync(CancellationToken ct) =>
        db.Orders
          .Where(o => o.TenantId == tenant.Tenant!.Id)
          .ToListAsync(ct);

    public async Task CreateAsync(Order order, CancellationToken ct)
    {
        order.TenantId = tenant.Tenant!.Id;
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }
}
```

For EF Core you can centralise the filter in `DbContext.OnModelCreating` using a global query filter:

```csharp
// Inject ITenantAccessor into DbContext via constructor
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantAccessor tenant) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Order>()
          .HasQueryFilter(o => o.TenantId == tenant.Tenant!.Id);
    }
}
```

> **Warning:** Global query filters are bypassed by `IgnoreQueryFilters()`. Make sure your team
> is aware of this escape hatch and that it's only used for administrative/reporting queries.

---

## Pattern 2 — Per-tenant connection string

Store a connection string in `Tenant.Claims` and resolve it at runtime to open a per-tenant
database connection.

```csharp
// Store tenants with per-tenant connection strings
builder.Services
    .AddPrimitivesMultitenancy()
    .WithInMemoryTenants(t =>
    {
        t.Add(new Tenant
        {
            Id   = "acme",
            Name = "Acme Corp",
            Claims = new Dictionary<string, string>
            {
                ["ConnectionString"] = "Server=acme-db;Database=AcmeApp;…",
            },
        });
    });

// Resolve at runtime
public class TenantDbContextFactory(ITenantAccessor tenant)
{
    public AppDbContext Create()
    {
        var connectionString = tenant.Tenant!.Claims["ConnectionString"];
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
```

---

## Pattern 3 — Separate schema per tenant

On SQL Server and PostgreSQL you can use schemas (`acme.Orders`, `bigco.Orders`) while keeping a
single database. Resolve the schema name from the tenant:

```csharp
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantAccessor tenant) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder mb)
    {
        var schema = tenant.Tenant?.Id ?? "public";
        mb.HasDefaultSchema(schema);
    }
}
```

---

## Pattern 4 — Feature flags from tenant claims

`Tenant.Claims` is a free-form `IReadOnlyDictionary<string, string>` — use it to store any
per-tenant configuration value:

```csharp
.WithInMemoryTenants(t =>
{
    t.Add(new Tenant
    {
        Id     = "acme",
        Claims = new Dictionary<string, string>
        {
            ["plan"]              = "enterprise",
            ["max-users"]         = "unlimited",
            ["feature-analytics"] = "true",
        },
    });
});
```

```csharp
public class FeatureService(ITenantAccessor tenant)
{
    public bool IsAnalyticsEnabled() =>
        tenant.Tenant?.Claims.TryGetValue("feature-analytics", out var v) == true
        && v == "true";
}
```

---

## Multi-tenancy security checklist

- Always filter by `tenant.Tenant!.Id` — never trust a tenant ID from a query parameter unless it
  has been validated against the resolved tenant.
- Enforce `RequireTenant = true` on all routes that handle tenant-specific data.
- Use `[Authorize]` in combination with `WithClaimResolver` so the tenant comes from a verified
  identity token, not a caller-supplied header.
- Test cross-tenant access: write a test that creates data for tenant A and asserts tenant B
  cannot read it.
