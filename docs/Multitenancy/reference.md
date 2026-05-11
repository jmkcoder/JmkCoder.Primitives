---
layout: default
library: multitenancy
title: Configuration Reference
description: Full API reference for Primitives.Multitenancy — options, interfaces, resolvers, and middleware.
permalink: /multitenancy/reference/
---

## `MultitenancyOptions`

Configure via `AddPrimitivesMultitenancy(o => { … })`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequireTenant` | `bool` | `false` | When `true`, requests that cannot be resolved to a known tenant are rejected. |
| `TenantNotFoundStatusCode` | `int` | `400` | HTTP status code returned when `RequireTenant` is `true` and no tenant is found. |
| `Tenants` | `List<Tenant>` | `[]` | Tenants seeded into `InMemoryTenantStore`. Populated by `.WithInMemoryTenants(…)`. |

---

## `Tenant`

```csharp
public sealed class Tenant
{
    public required string Id   { get; init; }
    public string?         Name { get; init; }
    public IReadOnlyDictionary<string, string> Claims { get; init; }
}
```

`Claims` is a case-insensitive string-to-string dictionary. Use it for arbitrary per-tenant metadata (feature flags, plan tier, connection strings, etc.).

---

## `AddPrimitivesMultitenancy`

```csharp
IServiceCollection.AddPrimitivesMultitenancy(
    Action<MultitenancyOptions>? configure = null)
    → MultitenancyBuilder
```

Registers:
- `IHttpContextAccessor`
- `ITenantResolver` → `CompositeTenantResolver` (tries all `ITenantResolverStrategy` singletons in order)
- `ITenantStore` → `InMemoryTenantStore` (reads `MultitenancyOptions.Tenants`)
- `ITenantAccessor` (scoped) → `TenantAccessor` (reads from `HttpContext.Items`)

---

## `MultitenancyBuilder` fluent API

All extension methods return `MultitenancyBuilder` for chaining.

### Resolver registration

| Method | Registers | Default key |
|--------|-----------|-------------|
| `.WithHeaderResolver(string? headerName)` | `HeaderTenantResolver` | `X-Tenant-Id` |
| `.WithHostResolver(Action<HostResolverOptions>? configure)` | `HostTenantResolver` | Leftmost subdomain |
| `.WithRouteValueResolver(string routeParameter)` | `RouteValueTenantResolver` | `tenantId` |
| `.WithClaimResolver(string claimType)` | `ClaimTenantResolver` | `tenant_id` |
| `.WithQueryStringResolver(string parameterName)` | `QueryStringTenantResolver` | `tenantId` |

### Store configuration

```csharp
// Seed in-memory tenants
builder.WithInMemoryTenants(Action<List<Tenant>> configureTenants)

// Replace InMemoryTenantStore with a custom implementation
builder.AddTenantStore<TStore>() where TStore : class, ITenantStore
```

---

## Resolver options

### `HostResolverOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SubdomainIndex` | `int` | `0` | Zero-based index of the subdomain segment to extract. `0` = leftmost. |
| `HostMap` | `Dictionary<string, string>` | `{}` | Full host → tenant identifier overrides. Case-insensitive. Takes precedence over subdomain extraction. |

### `HeaderResolverOptions`

| Property | Type | Default |
|----------|------|---------|
| `HeaderName` | `string` | `X-Tenant-Id` |

### `RouteValueResolverOptions`

| Property | Type | Default |
|----------|------|---------|
| `RouteParameter` | `string` | `tenantId` |

### `ClaimResolverOptions`

| Property | Type | Default |
|----------|------|---------|
| `ClaimType` | `string` | `tenant_id` |

### `QueryStringResolverOptions`

| Property | Type | Default |
|----------|------|---------|
| `ParameterName` | `string` | `tenantId` |

---

## Core interfaces

### `ITenantResolver`

```csharp
public interface ITenantResolver
{
    Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default);
}
```

Returns a tenant identifier string, or `null` if the request cannot be resolved.

### `ITenantResolverStrategy`

Marker interface. Implement this instead of `ITenantResolver` when creating strategies that
participate in the composite resolver. Register as `ITenantResolverStrategy` singleton or use
the `.WithXxxResolver()` builder methods.

### `ITenantStore`

```csharp
public interface ITenantStore
{
    Task<Tenant?> FindByIdentifierAsync(string identifier, CancellationToken ct = default);
}
```

### `ITenantAccessor`

```csharp
public interface ITenantAccessor
{
    Tenant? Tenant { get; }
}
```

Returns `null` outside a tenanted HTTP request or when no tenant was resolved.

---

## `UsePrimitivesMultitenancy`

```csharp
IApplicationBuilder.UsePrimitivesMultitenancy() → IApplicationBuilder
```

Adds `TenantResolutionMiddleware` to the pipeline. The middleware:

1. Calls `ITenantResolver.ResolveAsync` to obtain a tenant identifier.
2. Calls `ITenantStore.FindByIdentifierAsync` to load the `Tenant`.
3. Stores the `Tenant` in `HttpContext.Items[TenantResolutionMiddleware.TenantItemKey]`.
4. If `RequireTenant` is `true` and no tenant was resolved, writes the configured status code and short-circuits the pipeline.
5. Otherwise, calls the next middleware in the pipeline.

**Placement:** After `UseRouting()` (route values) and `UseAuthentication()` (claims); before `UseAuthorization()` and endpoint handlers.
