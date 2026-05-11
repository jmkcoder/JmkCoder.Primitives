---
layout: default
library: caching
permalink: /caching/
---

<div class="bd-hero">
  <h1>Primitives.Caching</h1>
  <p class="lead">
    Unified caching over in-memory, Redis, and distributed providers with automatic invalidation
    and cache-aside helpers built in. One interface — <code>ICacheService</code> — works across
    every backend without changing your application code.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Caching
  </div>
</div>

## The problem it solves

Most .NET caching code is littered with `IMemoryCache`/`IDistributedCache` calls scattered across
services. When you swap from in-memory to Redis you touch dozens of files. Expiry logic, key
construction, and tag-based invalidation live in different places and get duplicated.

`Primitives.Caching` gives you:

- **One interface** — `ICacheService` — with `GetOrSetAsync`, `SetAsync`, `GetAsync`, and `InvalidateAsync`.
- **Swappable backends** — in-memory, `IDistributedCache`, or Redis, chosen at registration time.
- **Cache-aside out of the box** — `GetOrSetAsync` handles the miss-fill-return cycle atomically.
- **Tag-based invalidation** — tag entries at write time, invalidate by tag at any point.

## Quick start

```csharp
// Program.cs — in-memory (single node)
builder.Services.AddPrimitivesCache(options =>
{
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
});
```

```csharp
// Inject and use
public class ProductService(ICacheService cache, ProductRepository db)
{
    public Task<Product?> GetAsync(int id, CancellationToken ct) =>
        cache.GetOrSetAsync(
            key:     $"product:{id}",
            factory: _ => db.FindAsync(id),
            options: new CacheEntryOptions
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(10),
                Tags = [$"product", $"product:{id}"],
            },
            cancellationToken: ct);
}
```

## Packages

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Caching</div>
    <p>Core abstractions, in-memory and distributed providers, cache-aside helpers.</p>
    <div class="install-cmd">dotnet add package Primitives.Caching</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Caching.Redis</div>
    <p>Redis provider with pub/sub cross-node invalidation and cluster support.</p>
    <div class="install-cmd">dotnet add package Primitives.Caching.Redis</div>
  </div>
</div>

## Design principles

**Single interface, multiple backends.** `ICacheService` is the only abstraction your code depends
on. Switching from in-memory to Redis requires changing one line in `Program.cs`.

**Cache-aside is the default.** `GetOrSetAsync` is the primary API. It handles the read → miss →
fill → store cycle for you, including propagating factory exceptions by default.

**Tagged invalidation.** Assign one or more string tags to each entry. Call
`InvalidateByTagAsync("product")` to atomically remove every entry carrying that tag — no need to
track keys manually.

**Key prefix namespacing.** Set `CacheOptions.KeyPrefix` to isolate entries between environments,
tenants, or application versions sharing the same backend.
