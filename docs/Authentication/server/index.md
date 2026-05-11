---
layout: default
library: authentication
title: Server Overview
description: The AspNetCore package adds REST token endpoints, JWT Bearer validation, a gRPC interceptor, and a SignalR hub filter.
permalink: /authentication/server/
---

## What is server-side authentication?

The `Primitives.Authentication.AspNetCore` package adds the **inbound** half of the authentication\nstory to your ASP.NET Core application. Once you can _issue_ JWTs (covered in\n[Getting Started]({{ '/authentication/getting-started/' | relative_url }})), you need to _validate_ them when\nclients send them back. This package provides:\n\n- **`POST /token` REST endpoints** \u2014 HTTP clients (browsers, mobile apps, other services) can\n  exchange credentials for tokens without you writing any controller code.\n- **JWT Bearer validation** \u2014 the standard `[Authorize]` attribute and `RequireAuthorization()`\n  extension method verify incoming JWT signatures using the same key that issued them.\n- **gRPC server interceptor** \u2014 validates the `Authorization` metadata on every inbound gRPC call.\n- **SignalR hub filter** \u2014 validates tokens at WebSocket connection time and on every hub method\n  invocation.\n- **Message queue middleware** \u2014 a broker-agnostic abstraction for validating tokens on inbound\n  messages.\n\nNone of these require extra code in your controllers or hub methods \u2014 they integrate at the\nmiddleware/interceptor layer.\n\n

---

## Registration

Call `AddPrimitivesAspNetCoreAuthentication()` **after** `AddJwtTokenIssuance()`:

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc(o => { ΓÇª })
    .AddJwtTokenIssuance(o => { ΓÇª });           // ΓåÉ must come first

builder.Services.AddPrimitivesAspNetCoreAuthentication(); // ΓåÉ registers gRPC + SignalR
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

See [Token Endpoints]({{ '/authentication/server/token-endpoints/' | relative_url }}) for the full route reference.

---

## What's in this section

| Page | Description |
|---|---|
| [Token Endpoints]({{ '/authentication/server/token-endpoints/' | relative_url }}) | REST routes for login, refresh, and revocation |
| [JWT Bearer Validation]({{ '/authentication/server/jwt-bearer/' | relative_url }}) | Protecting controllers and minimal API routes |
| [gRPC Interceptor]({{ '/authentication/server/grpc/' | relative_url }}) | `AuthenticationServerInterceptor` for all gRPC call types |
| [SignalR Hub Filter]({{ '/authentication/server/signalr/' | relative_url }}) | `AuthenticationHubFilter` for connection and method guards |
| [Message Queue]({{ '/authentication/server/message-queue/' | relative_url }}) | Abstract base for inbound message authentication |