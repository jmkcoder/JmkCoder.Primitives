---
layout: default
library: caching
title: Redis Provider
description: Configure Primitives.Caching.Redis for production-grade caching with cross-node tag invalidation and Redis Cluster support.
permalink: /caching/redis/
---

## Install

```bash
dotnet add package Primitives.Caching.Redis
```

## Register

```csharp
builder.Services.AddPrimitivesCacheRedis(
    configureRedis: options =>
    {
        options.Configuration         = "localhost:6379";
        options.DatabaseIndex         = 0;
        options.UsePubSubInvalidation = true;
        options.InvalidationChannel   = "myapp:cache:invalidation";
    },
    configureCache: options =>
    {
        options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
        options.KeyPrefix                 = "myapp";
    });
```

## Share a multiplexer

If your application already has `IConnectionMultiplexer` registered (e.g. for pub/sub or
Lua scripts), reuse it:

```csharp
// Register your multiplexer once
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect("localhost:6379"));

// Tell Primitives.Caching to use it
builder.Services.AddPrimitivesCacheRedis(options =>
{
    options.ConnectionMultiplexerFactory =
        sp => sp.GetRequiredService<IConnectionMultiplexer>();
});
```

## Redis Cluster

No special configuration is required. StackExchange.Redis handles cluster topology automatically
when you provide a cluster-aware connection string:

```csharp
options.Configuration = "redis-node1:6379,redis-node2:6379,redis-node3:6379";
```

## Redis Sentinel

```csharp
options.Configuration =
    "sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=mymaster";
```

## RedisCacheOptions reference

| Property | Default | Description |
|----------|---------|-------------|
| `Configuration` | `null` | StackExchange.Redis connection string. |
| `ConnectionMultiplexerFactory` | `null` | Factory for a shared `IConnectionMultiplexer`. Takes precedence over `Configuration`. |
| `DatabaseIndex` | `-1` | Redis database index (`-1` = default, usually DB 0). |
| `UsePubSubInvalidation` | `true` | Publish tag-invalidation events so all instances purge their local index. |
| `InvalidationChannel` | `"primitives:caching:invalidation"` | Pub/sub channel name. |

## How cross-node invalidation works

```
Instance A                          Redis
─────────────────────────────────────────────────────────
SetAsync("k", v, tags:["t"])  ──►  SADD "primitives:tag:t" "k"
                                   SET  "k" <json>

InvalidateByTagAsync("t")     ──►  SMEMBERS "primitives:tag:t"  → ["k"]
                                   DEL "k"
                                   DEL "primitives:tag:t"
                                   PUBLISH "primitives:caching:invalidation" "t"
                                              │
Instance B                                   ▼
─────────────────────────────────────────────────────────
                               (subscribed)  purge local tag index for "t"
```

Instance B's local tag index for `"t"` is cleared so stale key references are not accumulated
in memory.

## Health checks

You can add a Redis health check alongside Primitives.Caching to monitor connectivity:

```csharp
builder.Services
    .AddHealthChecks()
    .AddRedis("localhost:6379", name: "redis-cache");
```

Use `AspNetCore.HealthChecks.Redis` (via NuGet) for the `AddRedis` extension.

## Testing with Redis

For integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a
throwaway Redis instance:

```csharp
await using var redis = new RedisBuilder().Build();
await redis.StartAsync();

services.AddPrimitivesCacheRedis(options =>
{
    options.Configuration = redis.GetConnectionString();
});
```
