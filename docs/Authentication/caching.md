---
layout: default
library: authentication
title: Caching
description: Token results and refresh tokens are cached to avoid unnecessary round-trips. Swap the in-memory defaults for Redis or SQL Server in one line.
permalink: /authentication/caching/
---

## Why token caching matters

Every time `ITokenIssuanceService.AuthenticateAsync()` is called, the underlying strategy could
make a network round-trip to an identity provider (OIDC), perform a database lookup, or execute
some other potentially slow operation. Without caching, a busy service that authenticates on every
request would add that latency to every operation — and risk hitting rate limits imposed by
the identity provider.

Token caching solves this by storing the `AuthenticationResult` after the first successful
authentication and returning the cached result on subsequent calls until the token is about to
expire. The identity provider is only called once per token lifetime rather than once per request.

**The `EarlyExpiryBuffer` concept.** Suppose an access token expires at `15:00:00` and your cache
stores it until exactly that moment. A request that hits the cache at `14:59:55` gets the token,
but by the time the token reaches the downstream API (a few milliseconds later), it might be
considered expired due to clock skew. `EarlyExpiryBuffer` (default 30 seconds) causes the cache
to evict the result at `14:59:30` instead, ensuring there is always a safety margin.

---

## Overview

Two independent caches exist:

| Cache | Interface | What it stores | Default |
|---|---|---|---|
| Auth result cache | `IAuthenticationResultCache` | `AuthenticationResult` per strategy name | In-memory via `Primitives.Caching` (`ICacheService`) |
| Refresh token store | `IRefreshTokenStore` | `RefreshTokenEntry` records | In-memory (thread-safe dictionary) |

Both are automatically registered by `AddJwtTokenIssuance()`. The auth result cache is backed by
[`Primitives.Caching`]({{ '/caching/' | relative_url }}) (`ICacheService`), so the same provider
you configure for application-level caching is reused automatically.

---

## In-memory (default)

`AddResultCache()` registers the in-memory `Primitives.Caching` backend. No separate caching
package is required:

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … })
    .AddResultCache(o =>
    {
        o.EarlyExpiryBuffer = TimeSpan.FromSeconds(30); // default
    });
```

`EarlyExpiryBuffer`: cached results are evicted this long before the token's stated expiry to avoid
serving tokens that are about to expire to downstream callers.

The in-memory backend is suitable for:
- Single-instance applications
- Development and testing
- Worker services with a single process

---

## Redis backend

Add `Primitives.Caching.Redis` and register it **before** the authentication builder. The auth
result cache will automatically use the Redis `ICacheService` — no separate auth wiring required:

```csharp
// 1. Register Primitives.Caching.Redis (covers both app caching and auth result caching)
builder.Services.AddPrimitivesCacheRedis(
    configureRedis: o =>
    {
        o.Configuration          = config.GetConnectionString("Redis");
        o.UsePubSubInvalidation  = true;
    },
    configureCache: o =>
    {
        o.KeyPrefix = "myapp";
    });

// 2. Wire authentication as normal — it reuses the Redis ICacheService
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … })
    .AddResultCache(o => { o.EarlyExpiryBuffer = TimeSpan.FromSeconds(30); })
    .AddDistributedRefreshTokenStore();  // still uses IDistributedCache for refresh tokens
```

---

## Distributed cache (SQL Server, NCache, …)

For non-Redis distributed deployments use `AddDistributedResultCache()`, which switches the
auth result cache to the `IDistributedCache`-backed `Primitives.Caching` provider:

```csharp
// 1. Register any IDistributedCache provider
builder.Services.AddDistributedSqlServerCache(o =>
{
    o.ConnectionString = config.GetConnectionString("CacheDb");
    o.SchemaName       = "dbo";
    o.TableName        = "AppCache";
});

// 2. Replace the default in-memory implementations
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … })
    .AddDistributedResultCache()         // switches auth result cache to IDistributedCache
    .AddDistributedRefreshTokenStore();  // switches refresh token store to IDistributedCache
```

---

## Cache key prefixes

| Store | Key format | Example |
|---|---|---|
| Auth result cache | `prim:auth:{strategyName}` | `prim:auth:OIDC` |
| Refresh token store | `prim:rt:{token}` | `prim:rt:dGhpcyBpcyBh…` |

Both use absolute expiry — the TTL is the token's `ExpiresAt` minus `EarlyExpiryBuffer`.

---

## Known limitations of the distributed store

<div class="bd-callout bd-callout-warning">
<strong>No chain revocation across nodes.</strong> The distributed <code>IRefreshTokenStore</code> supports
individual token revocation (<code>RevokeAsync</code>) but does NOT propagate chain revocation
(invalidating all successors of a reused token) across nodes.
<br><br>
If full chain revocation is required in a multi-node deployment, implement a custom
<code>IRefreshTokenStore</code> backed by a shared atomic store (e.g. Redis with Lua scripts)
or use sticky sessions to ensure a token chain always resolves on the same node.
</div>

---

## `AuthenticationCacheOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `EarlyExpiryBuffer` | `TimeSpan` | `00:00:30` | Evict cached results this long before `ExpiresAt` |

---

## Custom implementation

You can replace the backing store at two levels:

**Option A — custom `ICacheService` provider (recommended)**

Implement a custom `ICacheService` and register it before the auth builder. Because the auth
result cache resolves its storage through `ICacheService`, your custom provider is picked up
automatically with no auth-specific code:

```csharp
services.AddSingleton<ICacheService, MyCustomCacheService>();

services
    .AddAuthentication()
    .AddResultCache();   // uses MyCustomCacheService
```

**Option B — custom `IAuthenticationResultCache`**

If you need auth-specific cache logic (e.g. a different key scheme or serialisation), implement
the interface directly and replace the registration:

```csharp
public sealed class MyAuthResultCache : IAuthenticationResultCache
{
    public Task<AuthenticationResult?> GetAsync(string key, CancellationToken ct) { … }
    public Task SetAsync(string key, AuthenticationResult result, CancellationToken ct) { … }
    public Task RemoveAsync(string key, CancellationToken ct) { … }
}

services.Replace(ServiceDescriptor.Singleton<IAuthenticationResultCache, MyAuthResultCache>());
```