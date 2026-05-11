---
layout: default
library: caching
title: Tag-based Invalidation
description: Tag cache entries at write time and invalidate groups of related entries with a single call.
permalink: /caching/invalidation/
---

## The problem with key-by-key invalidation

When a product is updated, you may need to remove:

- `product:42` — the single-item cache
- `product:list:page:1` — the first page of a product list
- `product:list:page:2` — the second page
- `search:results:shoes` — any search result that included the product

Tracking all those keys at write time is error-prone. Tags solve this cleanly.

## How tags work

When storing an entry you attach one or more string tags:

```csharp
await cache.SetAsync(
    key:     "product:42",
    value:   product,
    options: new CacheEntryOptions
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(10),
        Tags = ["product", "product:42"],
    });
```

When the product is updated, invalidate the entire group:

```csharp
await cache.InvalidateByTagAsync("product:42");
// Removes every entry tagged with "product:42"
```

Or invalidate the broader group to clear all products from cache:

```csharp
await cache.InvalidateByTagAsync("product");
```

## Tags with GetOrSetAsync

Tags work with `GetOrSetAsync` too:

```csharp
var page = await cache.GetOrSetAsync(
    key:     $"product:list:page:{pageNumber}",
    factory: _ => repo.GetPageAsync(pageNumber),
    options: new CacheEntryOptions
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(5),
        Tags = ["product"],          // invalidated whenever any product changes
    });
```

## Invalidation strategies

### Fine-grained — invalidate one entity

```csharp
// Tag with the entity's own key
Tags = [$"product:{product.Id}"]

// Later:
await cache.InvalidateByTagAsync($"product:{updatedProduct.Id}");
```

### Coarse-grained — invalidate a type

```csharp
// Tag every product entry
Tags = ["product"]

// Later (e.g. after a bulk import):
await cache.InvalidateByTagAsync("product");
```

### Multi-tag — entity belongs to multiple groups

```csharp
Tags = ["product", $"product:{product.Id}", $"category:{product.CategoryId}"]

// Invalidate just by category (e.g. a category rename):
await cache.InvalidateByTagAsync($"category:{categoryId}");
```

## Direct key invalidation

For single known keys, use `InvalidateAsync`:

```csharp
await cache.InvalidateAsync($"product:{id}");
```

## Backend behaviour

| Backend | Tag storage | Cross-node? |
|---------|-------------|-------------|
| In-memory | In-process dictionary | No (single node only) |
| Distributed | In-process dictionary | No — use Redis for cross-node |
| Redis | Redis `SET` per tag + optional pub/sub | Yes, with `UsePubSubInvalidation = true` |

When using the Redis provider with `UsePubSubInvalidation = true`, calling `InvalidateByTagAsync`
publishes a message to all subscribers. Every running instance removes the tag from its local
in-process index and the Redis `SET` is deleted — so newly started nodes also see a clean state.
