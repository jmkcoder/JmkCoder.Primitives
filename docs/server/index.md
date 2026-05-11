---
layout: default
title: Server Overview
description: The AspNetCore package adds REST token endpoints, JWT Bearer validation, a gRPC interceptor, and a SignalR hub filter.
permalink: /server/
---

## Package

```bash
dotnet add package Primitives.Authentication.AspNetCore
```

The AspNetCore package depends on `Primitives.Authentication` — you only need to reference the AspNetCore package; the core package is pulled in automatically.

---

## Registration

Call `AddPrimitivesAspNetCoreAuthentication()` **after** `AddJwtTokenIssuance()`:

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … });           // ← must come first

builder.Services.AddPrimitivesAspNetCoreAuthentication(); // ← registers gRPC + SignalR
```

<div class="bd-callout bd-callout-danger">
<strong>Order matters.</strong> <code>AddPrimitivesAspNetCoreAuthentication()</code> resolves <code>IJwtTokenValidator</code>
at first DI resolution. If <code>AddJwtTokenIssuance()</code> has not been called, the application will throw
an <code>InvalidOperationException</code> with a clear message explaining the missing registration.
</div>

---

## What gets registered

| Service | Type | Description |
|---|---|---|
| `AuthenticationServerInterceptor` | Singleton | Validates Bearer tokens in inbound gRPC calls |
| `AuthenticationHubFilter` | Singleton | Validates Bearer tokens in SignalR hub connections and method invocations |

---

## Minimal API token endpoints

Expose POST `/token`, `/token/refresh`, and `/token/revoke` with a single call:

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPrimitivesTokenEndpoints();          // mounts at /token
// or: app.MapPrimitivesTokenEndpoints("/auth");  // custom prefix
```

See [Token Endpoints]({{ '/server/token-endpoints/' | relative_url }}) for the full route reference.

---

## What's in this section

| Page | Description |
|---|---|
| [Token Endpoints]({{ '/server/token-endpoints/' | relative_url }}) | REST routes for login, refresh, and revocation |
| [JWT Bearer Validation]({{ '/server/jwt-bearer/' | relative_url }}) | Protecting controllers and minimal API routes |
| [gRPC Interceptor]({{ '/server/grpc/' | relative_url }}) | `AuthenticationServerInterceptor` for all gRPC call types |
| [SignalR Hub Filter]({{ '/server/signalr/' | relative_url }}) | `AuthenticationHubFilter` for connection and method guards |
| [Message Queue]({{ '/server/message-queue/' | relative_url }}) | Abstract base for inbound message authentication |
