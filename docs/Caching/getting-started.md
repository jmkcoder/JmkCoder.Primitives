---
layout: default
library: caching
title: Installation
description: Add Primitives.Caching to your .NET 8 project and register the right provider for your infrastructure.
permalink: /caching/getting-started/
---

## Requirements

- .NET 8 or later
- For Redis: a reachable Redis 6+ instance (standalone, Sentinel, or Cluster)

## Install the packages

Add the core package to every project that needs caching:

```bash
dotnet add package Primitives.Caching
```

If you are using Redis as the backend:

```bash
dotnet add package Primitives.Caching.Redis
```

## Register the provider

Choose **one** of the three registration methods in `Program.cs` depending on your infrastructure.

### In-memory (single node)

Best for development, tests, or single-instance services where cached data does not need to survive
a restart or be shared across nodes.

```csharp
builder.Services.AddPrimitivesCache(options =>
{
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
    options.KeyPrefix = "myapp";
});
```

### Distributed (SQL Server, NCache, Cosmos, etc.)

Use any `IDistributedCache` adapter. Register it first, then call `AddPrimitivesCacheDistributed`.

```csharp
// SQL Server distributed cache (Microsoft.Extensions.Caching.SqlServer)
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("CacheDb");
    options.SchemaName = "dbo";
    options.TableName  = "AppCache";
});

builder.Services.AddPrimitivesCacheDistributed(options =>
{
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(10);
});
```

### Redis

```csharp
builder.Services.AddPrimitivesCacheRedis(
    configureRedis: options =>
    {
        options.Configuration        = "localhost:6379";
        options.DatabaseIndex        = 0;
        options.UsePubSubInvalidation = true;   // cross-node tag invalidation
    },
    configureCache: options =>
    {
        options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
        options.KeyPrefix                 = "myapp";
    });
```

If you already have an `IConnectionMultiplexer` registered (e.g. from a shared StackExchange.Redis
setup), provide it via the factory property instead:

```csharp
builder.Services.AddPrimitivesCacheRedis(options =>
{
    options.ConnectionMultiplexerFactory = sp =>
        sp.GetRequiredService<IConnectionMultiplexer>();
});
```

## Inject and use

Once registered, inject `ICacheService` wherever you need caching:

```csharp
public class OrderService(ICacheService cache, IOrderRepository repo)
{
    public Task<Order?> GetOrderAsync(Guid id, CancellationToken ct) =>
        cache.GetOrSetAsync(
            key:     $"order:{id}",
            factory: _ => repo.FindAsync(id),
            options: new CacheEntryOptions
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(15),
                Tags = ["order", $"order:{id}"],
            },
            cancellationToken: ct);
}
```

## Configuration reference

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultAbsoluteExpiration` | `5m` | Applied when no per-entry expiry is set. |
| `PropagateFactoryExceptions` | `true` | When `true`, factory exceptions bubble to the caller. Set `false` to return `default` instead. |
| `KeyPrefix` | `""` | Prepended to every key as `"{prefix}:{key}"`. |

For Redis-specific options see [Redis Provider]({{ '/caching/redis/' | relative_url }}).
