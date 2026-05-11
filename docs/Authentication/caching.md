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
| Auth result cache | `IAuthenticationResultCache` | `AuthenticationResult` per strategy name | In-memory (`IMemoryCache`) |
| Refresh token store | `IRefreshTokenStore` | `RefreshTokenEntry` records | In-memory (thread-safe dictionary) |

Both are automatically registered by `AddJwtTokenIssuance()`. To replace them with distributed implementations, call the relevant builder methods after `AddJwtTokenIssuance`.

---

## In-memory (default)

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

`EarlyExpiryBuffer`: cached results are evicted this long before the token's stated expiry to avoid serving tokens that are about to expire to downstream callers.

The default in-memory implementation is suitable for:
- Single-instance applications
- Development and testing
- Worker services with a single process

---

## Distributed cache (Redis, SQL Server, …)

For multi-instance deployments (Kubernetes, Azure App Service, etc.), replace both stores with `IDistributedCache`-backed implementations:

```csharp
// 1. Register any IDistributedCache provider
builder.Services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = config.GetConnectionString("Redis");
});

// 2. Replace the default in-memory implementations
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … })
    .AddDistributedResultCache()         // replaces IAuthenticationResultCache
    .AddDistributedRefreshTokenStore();  // replaces IRefreshTokenStore
```

Both methods use `services.Replace(…)` so they override the defaults registered by `AddJwtTokenIssuance()` without needing to remove them first.

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

Implement either interface to provide a completely custom backing store:

```csharp
public sealed class RedisAuthResultCache : IAuthenticationResultCache
{
    public Task<AuthenticationResult?> GetAsync(string key, CancellationToken ct) { … }
    public Task SetAsync(string key, AuthenticationResult result, CancellationToken ct) { … }
    public Task RemoveAsync(string key, CancellationToken ct) { … }
}

// Register it as a singleton, replacing the default
services.Replace(ServiceDescriptor.Singleton<IAuthenticationResultCache, RedisAuthResultCache>());
```