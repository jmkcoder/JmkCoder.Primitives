---
layout: default
library: caching
title: Configuration Reference
description: Complete reference for all CacheOptions and RedisCacheOptions properties.
permalink: /caching/reference/
---

## CacheOptions

Applies globally to all entries unless overridden per-call via `CacheEntryOptions`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultAbsoluteExpiration` | `TimeSpan` | `00:05:00` | Absolute expiry applied when no per-entry expiry is provided. |
| `PropagateFactoryExceptions` | `bool` | `true` | When `true`, exceptions from the `factory` delegate in `GetOrSetAsync` bubble to the caller. When `false`, a warning is logged and `default(T)` is returned. |
| `KeyPrefix` | `string` | `""` | String prepended to every key as `"{prefix}:{key}"`. Useful for namespacing entries across tenants, environments, or app versions. |

### Example

```csharp
builder.Services.AddPrimitivesCache(options =>
{
    options.DefaultAbsoluteExpiration  = TimeSpan.FromMinutes(10);
    options.PropagateFactoryExceptions = false;
    options.KeyPrefix                  = $"myapp:{env.EnvironmentName}";
});
```

---

## CacheEntryOptions

Per-entry settings passed to `SetAsync` or `GetOrSetAsync`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AbsoluteExpiration` | `TimeSpan?` | `null` | Entry expires this long after it is stored. Overrides `CacheOptions.DefaultAbsoluteExpiration`. |
| `SlidingExpiration` | `TimeSpan?` | `null` | Entry expires this long after its last read. Mutually exclusive with `AbsoluteExpiration`. |
| `Tags` | `IReadOnlyList<string>` | `[]` | Tags for group invalidation via `InvalidateByTagAsync`. |

> When both `AbsoluteExpiration` and `SlidingExpiration` are set, `AbsoluteExpiration` takes
> precedence.

### Example

```csharp
var options = new CacheEntryOptions
{
    AbsoluteExpiration = TimeSpan.FromMinutes(15),
    Tags               = ["product", $"product:{id}"],
};
```

---

## RedisCacheOptions

`Primitives.Caching.Redis` only.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Configuration` | `string?` | `null` | StackExchange.Redis connection string (e.g. `"localhost:6379"`). |
| `ConnectionMultiplexerFactory` | `Func<IServiceProvider, IConnectionMultiplexer>?` | `null` | Factory for a shared multiplexer. Takes precedence over `Configuration`. |
| `DatabaseIndex` | `int` | `-1` | Redis database (`-1` = server default, usually 0). |
| `UsePubSubInvalidation` | `bool` | `true` | Publish tag-invalidation to all subscribed instances via Redis pub/sub. |
| `InvalidationChannel` | `string` | `"primitives:caching:invalidation"` | Pub/sub channel name. Change this if you run multiple independent apps against the same Redis server. |

---

## ICacheService API

```csharp
public interface ICacheService
{
    // Cache-aside: return cached value or call factory, store, and return
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    // Read only
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    // Write
    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    // Remove one entry
    Task InvalidateAsync(
        string key,
        CancellationToken cancellationToken = default);

    // Remove all entries sharing a tag
    Task InvalidateByTagAsync(
        string tag,
        CancellationToken cancellationToken = default);
}
```
