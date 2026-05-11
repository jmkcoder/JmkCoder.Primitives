---
layout: default
library: authentication
title: Token Endpoints
description: Expose POST /token, /token/refresh, and /token/revoke from any ASP.NET Core minimal API application.
permalink: /authentication/server/token-endpoints/
---

## What is a token endpoint?

A _token endpoint_ is an HTTP route that accepts credentials and returns a signed token. The term
comes from [OAuth 2.0 (RFC 6749 §3.2)](https://datatracker.ietf.org/doc/html/rfc6749#section-3.2),
which defines a standard interface for this exchange.

Without `MapPrimitivesTokenEndpoints()`, your service can only issue tokens programmatically
(via `ITokenIssuanceService`). That is fine for service-to-service calls where the caller injects
the service and calls it directly. But when your clients are external — a browser SPA, a mobile
app, or a CLI tool written in Python — they need an HTTP API to authenticate against.

`MapPrimitivesTokenEndpoints()` gives you the full lifecycle over HTTP in a single call:

```
curl POST /token        →  receive accessToken + refreshToken
curl POST /token/refresh →  exchange refreshToken for a new pair (old one revoked)
curl POST /token/revoke  →  invalidate a refresh token immediately (logout)
```



## Registration

```csharp
// After app.UseAuthentication() / app.UseAuthorization()
app.MapPrimitivesTokenEndpoints();            // mounts at /token (default)
app.MapPrimitivesTokenEndpoints("/auth");     // or a custom prefix
```

All three endpoints call `.AllowAnonymous()` — the credential check happens inside the strategy
itself, not at the HTTP middleware layer. This is intentional: requiring `[Authorize]` on a login
endpoint creates a circular dependency (you need a token to get a token).

<div class="bd-callout bd-callout-warning">
<strong>Apply rate limiting in production.</strong> Without rate limiting, <code>POST /token</code>
can be used for credential-stuffing attacks — automated tools that try thousands of
username/password combinations per second. Use <code>builder.Services.AddRateLimiter()</code>
with a fixed-window or sliding-window policy:
<pre><code class="language-csharp">builder.Services.AddRateLimiter(o =&gt;
    o.AddFixedWindowLimiter("token", p =&gt;
    {
        p.Window          = TimeSpan.FromMinutes(1);
        p.PermitLimit     = 10;
        p.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    }));

// Apply to the token endpoint
app.MapPrimitivesTokenEndpoints()
   .RequireRateLimiting("token");
</code></pre>
</div>

---

## Routes

| Method | Path | Request body | Success | Error |
|---|---|---|---|---|
| `POST` | `/token` | `TokenRequest` | `200 TokenResponse` | `401 Unauthorized` |
| `POST` | `/token/refresh` | `RefreshRequest` | `200 TokenResponse` | `401 Unauthorized` |
| `POST` | `/token/revoke` | `RevokeRequest` | `204 No Content` | `400 Bad Request` |

---

## `POST /token` — authenticate

Issue a new access token and refresh token using any registered strategy.

**Request body:**

```json
{ "strategyName": "OIDC" }
```

**Response body (`TokenResponse`):**

```json
{
  "accessToken":  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9…",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4",
  "tokenType":    "Bearer",
  "expiresAt":    "2026-05-10T15:00:00+00:00"
}
```

**Example — cURL:**

```bash
curl -X POST https://myapp.example.com/token \
     -H "Content-Type: application/json" \
     -d '{"strategyName":"OIDC"}'
```

**Example — .NET `HttpClient`:**

```csharp
var response = await http.PostAsJsonAsync("/token",
    new { strategyName = "OIDC" });

var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
```

---

## `POST /token/refresh` — rotate refresh token

Exchange a valid refresh token for a new access token and a **new** refresh token. The old refresh token is revoked immediately.

**Request body:**

```json
{ "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4" }
```

**Response:** same `TokenResponse` shape as above.

<div class="bd-callout bd-callout-warning">
<strong>Reuse detection:</strong> If a refresh token that has already been rotated is presented again,
the entire chain of successor tokens is revoked. Clients must securely store the latest refresh token
and never present an old one after rotation.
</div>

---

## `POST /token/revoke` — revoke refresh token

Immediately invalidate a refresh token so it can no longer be used.

**Request body:**

```json
{ "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4" }
```

**Response:** `204 No Content` on success; `400 Bad Request` if the token is not found.

---

## `TokenResponse` properties

| Property | Type | Description |
|---|---|---|
| `accessToken` | `string` | Signed HS256 JWT |
| `refreshToken` | `string?` | URL-safe random refresh token |
| `tokenType` | `string` | Always `"Bearer"` |
| `expiresAt` | `DateTimeOffset?` | UTC expiry of the access token |

---

## Security recommendations

- Serve these endpoints over **HTTPS only** — never HTTP in production.
- Apply **rate limiting** (`AddRateLimiter`) to `/token` to prevent credential stuffing.
- Store the `SigningKey` in a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.) and rotate it periodically.
- Revoke refresh tokens on logout via `POST /token/revoke`.