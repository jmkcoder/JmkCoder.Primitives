---
layout: default
title: JWT Bearer Validation
description: Use AddPrimitivesJwtBearer to protect controllers and minimal API routes with tokens issued by this library.
permalink: /server/jwt-bearer/
---

## Overview

`AddPrimitivesJwtBearer` is a thin wrapper around `AddJwtBearer` that pre-configures:

- **HS256 validation** using the same `SigningKey` you configured in `AddJwtTokenIssuance`
- **Issuer and audience** validation
- **SignalR query-string token support** (`?access_token=…`) for browser clients

---

## Registration

```csharp
builder.Services
    .AddAuthentication()
    .AddPrimitivesJwtBearer(o =>
    {
        o.Issuer    = "https://myapp.example.com";
        o.Audience  = "https://myapi.example.com";
        o.SigningKey = builder.Configuration["Jwt:SigningKey"]!;
    });

// Always add both middleware to the pipeline
app.UseAuthentication();
app.UseAuthorization();
```

<div class="bd-callout bd-callout-tip">
<strong>Same options, two places.</strong> The options you pass here must exactly match the
<code>Issuer</code>, <code>Audience</code>, and <code>SigningKey</code> you configured in
<code>AddJwtTokenIssuance()</code>. Consider binding both from the same configuration section.
</div>

---

## Protecting routes

Once registered, use the standard ASP.NET Core `[Authorize]` attribute or `RequireAuthorization()`:

**Minimal API:**

```csharp
app.MapGet("/orders", (ClaimsPrincipal user) =>
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Results.Ok(new { subject, orders = GetOrders(subject) });
})
.RequireAuthorization();
```

**Controller:**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(GetOrders(subject));
    }
}
```

---

## Reading claims

The JWT issued by this library always contains:

| Claim | JWT field | Description |
|---|---|---|
| `sub` | `ClaimTypes.NameIdentifier` | Authenticated subject |
| `iss` | — | Issuer |
| `aud` | — | Audience |
| `exp` | — | Expiry (Unix timestamp) |
| `iat` | — | Issued-at |
| Custom claims | varies | Strategy-specific claims passed via `AuthenticationResult.Claims` |

```csharp
var subject = User.FindFirstValue(ClaimTypes.NameIdentifier); // "sub"
var custom  = User.FindFirstValue("cert_thumbprint");          // custom claim
```

---

## Customising the scheme

To register a non-default scheme (e.g. for multiple APIs on one host):

```csharp
builder.Services
    .AddAuthentication()
    .AddPrimitivesJwtBearer(
        o => { … },
        scheme: "InternalApi");   // default is JwtBearerDefaults.AuthenticationScheme

// Then protect routes with the named scheme:
app.MapGet("/internal/data", …).RequireAuthorization("InternalApi");
```

---

## Token validation flow

1. ASP.NET Core extracts the `Authorization: Bearer <token>` header.
2. `JwtBearerHandler` calls `IJwtTokenValidator.ValidateAsync(token)`.
3. The validator checks signature, issuer, audience, and expiry.
4. On success, `HttpContext.User` is populated with the token's claims.
5. On failure, the response is `401 Unauthorized`.
