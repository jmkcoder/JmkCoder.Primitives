# Primitives.Caching

Unified caching over in-memory, Redis, and distributed providers with automatic invalidation and cache-aside helpers built in.

## Packages

| Package | Description |
|---------|-------------|
| `Primitives.Caching` | Core abstractions, in-memory provider, cache-aside helpers, and automatic invalidation. |
| `Primitives.Caching.Redis` | Redis provider built on StackExchange.Redis. |

## Quick start

```bash
dotnet add package Primitives.Caching
dotnet add package Primitives.Caching.Redis   # optional — Redis provider
```

```csharp
// Program.cs
builder.Services.AddPrimitivesCache(options =>
{
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
});

// Redis (optional)
builder.Services.AddPrimitivesCacheRedis(options =>
{
    options.Configuration = "localhost:6379";
});
```

```csharp
// Cache-aside in a service
public class ProductService(ICacheService cache)
{
    public Task<Product> GetProductAsync(int id, CancellationToken ct) =>
        cache.GetOrSetAsync(
            key:     $"product:{id}",
            factory: () => _db.Products.FindAsync(id, ct).AsTask(),
            expiry:  TimeSpan.FromMinutes(10),
            ct:      ct);
}
```

## Providers

- **In-memory** — `IMemoryCache`-backed, zero infrastructure, good for single-node apps.
- **Distributed** — `IDistributedCache`-backed, works with any MS distributed cache adapter.
- **Redis** — `Primitives.Caching.Redis` wraps StackExchange.Redis for full Redis feature set (pub/sub invalidation, key expiry, cluster support).

## License

MIT
