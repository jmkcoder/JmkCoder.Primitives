---
layout: default
library: authentication
title: JWT Bearer Validation
description: Use AddPrimitivesJwtBearer to protect controllers and minimal API routes with tokens issued by this library.
permalink: /authentication/server/jwt-bearer/
---

## What is JWT Bearer validation?

When your API receives a request with `Authorization: Bearer <token>`, it needs to answer three
questions before serving the response:

1. **Is this token genuine?** — did _this_ server sign it, or did someone forge it?
2. **Has it expired?** — tokens are time-limited; an old token should not grant access.
3. **Is it for this API?** — a token issued to service A should not be accepted by service B.

JWT validation answers all three locally — **no network call** to the issuer or identity provider
is needed. The signature is verified cryptographically using the shared signing key. The `exp`,
`iss`, and `aud` claims are checked against the expected values. The entire validation happens in
microseconds, in memory.

This is in contrast to _opaque tokens_ (used by OAuth 2.0 introspection), where the server must
call the identity provider on every request to check whether the token is still valid. JWTs
trade revocation flexibility for speed and independence.

**Issuance vs validation:**
- `AddJwtTokenIssuance()` is about creating tokens — outbound.
- `AddPrimitivesJwtBearer()` is about accepting tokens — inbound.

If your service both _issues_ tokens (is an auth server) and _validates_ them (is an API server),
you need both.

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