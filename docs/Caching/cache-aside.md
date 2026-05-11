---
layout: default
library: caching
title: Cache-aside Pattern
description: Use GetOrSetAsync to implement the cache-aside pattern without boilerplate.
permalink: /caching/cache-aside/
---

## What is cache-aside?

Cache-aside (also called *lazy loading*) is the most common caching pattern:

```
1. Application asks cache for value.
2. Cache hit  → return it immediately.
3. Cache miss → fetch from source, store in cache, return.
```

`Primitives.Caching` builds this pattern directly into `ICacheService.GetOrSetAsync` so you never
write the check-then-fetch logic yourself.

## Basic usage

```csharp
var product = await cache.GetOrSetAsync(
    key:     $"product:{id}",
    factory: async ct => await db.Products.FindAsync(id, ct),
    options: new CacheEntryOptions
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(10),
    });
```

`GetOrSetAsync` is:

- **Thread-safe** — concurrent requests for the same missing key each call the factory independently
  then race to set the value; the first writer wins and subsequent readers see the cached copy.
- **Strongly typed** — returns `T?`; no casting needed.
- **Exception-propagating by default** — if `factory` throws, the exception propagates to the
  caller so a downstream error is never silently swallowed.

## Disable factory exception propagation

If you want a cache miss to return `default` rather than throw (e.g. for non-critical data):

```csharp
// Program.cs
builder.Services.AddPrimitivesCache(options =>
{
    options.PropagateFactoryExceptions = false;
});
```

With this set, a faulting factory logs a warning and returns `default(T)`.

## Per-entry expiry

```csharp
var result = await cache.GetOrSetAsync(
    key:     "dashboard:stats",
    factory: _ => statsService.ComputeAsync(),
    options: new CacheEntryOptions
    {
        AbsoluteExpiration = TimeSpan.FromSeconds(30), // short for frequently changing data
    });
```

## Sliding expiry

```csharp
var session = await cache.GetOrSetAsync(
    key:     $"session:{token}",
    factory: _ => sessionStore.LoadAsync(token),
    options: new CacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(20),
    });
```

Sliding expiry resets the timer each time the entry is read. It is useful for session data where
inactivity should expire the entry but activity should keep it alive.

> **Note:** `AbsoluteExpiration` and `SlidingExpiration` are mutually exclusive. If both are set,
> absolute expiration takes precedence.

## Manual get and set

For cases where you need finer control:

```csharp
// Read only — no fill on miss
var cached = await cache.GetAsync<Product>($"product:{id}");
if (cached is null)
{
    // Not in cache — do something else
}

// Write directly
await cache.SetAsync(
    key:     $"product:{id}",
    value:   product,
    options: new CacheEntryOptions { AbsoluteExpiration = TimeSpan.FromMinutes(10) });
```

## Pattern: read-through wrapper

Encapsulate cache-aside in a repository decorator to keep caching logic out of your service layer:

```csharp
public class CachingProductRepository(
    IProductRepository inner,
    ICacheService       cache) : IProductRepository
{
    public Task<Product?> FindAsync(int id, CancellationToken ct) =>
        cache.GetOrSetAsync(
            key:     $"product:{id}",
            factory: _ => inner.FindAsync(id, ct),
            options: new CacheEntryOptions
            {
                AbsoluteExpiration = TimeSpan.FromMinutes(10),
                Tags = ["product", $"product:{id}"],
            },
            cancellationToken: ct);

    public async Task UpdateAsync(Product product, CancellationToken ct)
    {
        await inner.UpdateAsync(product, ct);
        await cache.InvalidateAsync($"product:{product.Id}", ct);
    }
}
```

Register it in DI:

```csharp
builder.Services.AddScoped<IProductRepository, CachingProductRepository>();
builder.Services.AddScoped<ProductRepository>(); // the real inner implementation
```
