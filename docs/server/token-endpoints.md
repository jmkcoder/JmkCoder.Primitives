---
layout: default
title: Token Endpoints
description: Expose POST /token, /token/refresh, and /token/revoke from any ASP.NET Core minimal API application.
permalink: /server/token-endpoints/
---

## Registration

```csharp
// After app.UseAuthentication() / app.UseAuthorization()
app.MapPrimitivesTokenEndpoints();            // mounts at /token (default)
app.MapPrimitivesTokenEndpoints("/auth");     // or a custom prefix
```

All three endpoints call `.AllowAnonymous()` — the credential check happens inside the strategy itself, not at the HTTP middleware layer.

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
