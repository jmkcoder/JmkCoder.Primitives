---
layout: default
library: multitenancy
title: Resolvers
description: Host, header, route value, claim, and query string resolvers — how to configure and combine them.
permalink: /multitenancy/resolvers/
---

## How resolvers work

`ITenantResolver` is responsible for returning a tenant **identifier** string from the current
`HttpContext`. The identifier is passed to `ITenantStore.FindByIdentifierAsync` to load the full
`Tenant` object.

When multiple strategies are registered, `CompositeTenantResolver` (the default `ITenantResolver`)
tries them **in registration order** and returns the first non-null result.

---

## Header resolver

Reads a tenant identifier from an HTTP request header.

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithHeaderResolver();               // default: X-Tenant-Id

// Custom header name
    .WithHeaderResolver("X-Account-Id");
```

```
GET /api/orders HTTP/1.1
X-Tenant-Id: acme
```

**Best for:** API-to-API calls, internal services, CLI tools making API requests.

---

## Host resolver

Extracts the tenant from the `Host` header — either by subdomain index or via an explicit host map.

```csharp
// Subdomain: acme.example.com → "acme"
builder.Services
    .AddPrimitivesMultitenancy()
    .WithHostResolver();                // default: leftmost subdomain (index 0)

// Non-zero index: www.acme.example.com → "acme" (index 1)
    .WithHostResolver(o => o.SubdomainIndex = 1);

// Explicit host map (takes precedence over subdomain extraction)
    .WithHostResolver(o =>
    {
        o.HostMap["acme.com"]  = "acme";
        o.HostMap["bigco.com"] = "bigco";
    });
```

**Best for:** SaaS products with per-tenant subdomains (`acme.yourapp.com`), custom domains.

---

## Route value resolver

Reads the tenant from a route parameter. **Requires `UseRouting` to run before `UsePrimitivesMultitenancy`.**

```csharp
// Route: /api/{tenantId}/orders
builder.Services
    .AddPrimitivesMultitenancy()
    .WithRouteValueResolver();              // default parameter: "tenantId"

// Custom parameter name
    .WithRouteValueResolver("account");     // route: /api/{account}/orders
```

**Best for:** REST APIs with the tenant embedded in the URL path.

---

## Claim resolver

Reads the tenant from the authenticated user's JWT claim. **Requires `UseAuthentication` to run before `UsePrimitivesMultitenancy`.**

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithClaimResolver();               // default: "tenant_id" claim

// Custom claim type
    .WithClaimResolver("extension_TenantId");
```

JWT payload example:
```json
{ "sub": "user123", "tenant_id": "acme" }
```

**Best for:** Applications where tenant membership is encoded in the identity token. Avoids
a round-trip to the tenant store to map a header value to a tenant — the claim is already
authoritative.

---

## Query string resolver

Reads the tenant from a URL query parameter.

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithQueryStringResolver();             // default: ?tenantId=

// Custom parameter name
    .WithQueryStringResolver("account");    // ?account=acme
```

**Best for:** Development and testing only. Not recommended in production APIs — query parameters are
logged, cached, and shared more easily than headers.

---

## Combining resolvers (priority order)

Register multiple strategies to cover different clients and fall back gracefully:

```csharp
builder.Services
    .AddPrimitivesMultitenancy()
    .WithClaimResolver()        // 1st — authenticated users (most authoritative)
    .WithHostResolver()         // 2nd — subdomain (unauthenticated public routes)
    .WithHeaderResolver();      // 3rd — explicit override for service-to-service
```

The composite resolver stops at the first non-null result.

---

## Custom resolver

Implement `ITenantResolverStrategy` to plug in any logic:

```csharp
public sealed class ApiKeyTenantResolver : ITenantResolverStrategy
{
    private readonly ApiKeyStore _keys;

    public ApiKeyTenantResolver(ApiKeyStore keys) => _keys = keys;

    public async Task<string?> ResolveAsync(HttpContext context, CancellationToken ct)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (apiKey is null) return null;
        return await _keys.GetTenantIdAsync(apiKey, ct);
    }
}

// Register alongside built-in strategies
builder.Services.AddSingleton<ITenantResolverStrategy, ApiKeyTenantResolver>();
```
