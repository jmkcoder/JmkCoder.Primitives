---
layout: default
library: authentication
title: API Key
description: Authenticate with a static shared secret. Three delivery modes — custom header, Bearer token, or URL query parameter.
permalink: /authentication/strategies/api-key/
---

## Overview

An API key is a shared secret that a client presents on every request to prove it is an authorized
caller. Unlike user authentication (OIDC, Username/Password), API keys represent _applications_
or _integrations_ — there is no user context or session involved.

The API Key strategy does one thing: it holds a configured secret and formats it for the chosen
delivery mechanism. When `AuthenticateAsync()` is called, it returns the key in the specified
format so the caller (or the Primitives HTTP client handler) can attach it to an outbound request.

**Use this strategy when:**
- Integrating with third-party APIs that authenticate via a header (e.g. `X-API-Key`, `Authorization: ApiKey …`)
- Exposing your own API to trusted partners via a shared secret
- Writing webhooks or event processors that receive a pre-shared token

---

## Registration

```csharp
services.AddAuthentication()
    .AddApiKey(o =>
    {
        o.ApiKey    = configuration["ApiKey:Secret"]!;  // load from secrets manager
        o.Placement = ApiKeyPlacement.Header;             // default
        o.KeyName   = "X-API-Key";                       // default header name
    });
```

For multiple API keys — each partner or integration gets its own named registration:

```csharp
.AddApiKey("PartnerA", o =>
{
    o.ApiKey   = configuration["Partners:A:Key"]!;
    o.KeyName  = "X-Partner-A-Key";
})
.AddApiKey("PartnerB", o =>
{
    o.ApiKey      = configuration["Partners:B:Key"]!;
    o.Placement   = ApiKeyPlacement.BearerToken;
})
```

---

## Placement modes

The `Placement` option controls where the key is delivered. Choose based on what the target API expects.

### `Header` (default)

The key is placed in a named HTTP header. This is the most common and secure placement — headers
are not logged by most reverse proxies or CDNs by default.

```
X-API-Key: your-secret-key
```

With an optional prefix (some APIs expect `ApiKey ` or `Token ` before the value):

```csharp
o.HeaderPrefix = "ApiKey ";
// → X-API-Key: ApiKey your-secret-key
```

### `BearerToken`

The key is returned as a standard Bearer token in the `Authorization` header. Use this when the
target API validates via the standard `Authorization: Bearer …` header but uses an API key rather
than a JWT.

```
Authorization: Bearer your-secret-key
```

```csharp
o.Placement = ApiKeyPlacement.BearerToken;
```

### `QueryParameter`

The key is returned as the `AccessToken` value. The caller is responsible for appending it to the
URL as a query parameter. **Avoid this in production** — query parameters appear in server access
logs, browser history, CDN logs, and referrer headers.

```
https://api.example.com/resource?api_key=your-secret-key
```

```csharp
o.Placement = ApiKeyPlacement.QueryParameter;
o.KeyName   = "api_key";  // the query parameter name
// Caller appends: ?api_key={result.AccessToken}
```

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `ApiKey` | `string` | ✅ | — | The secret key value. Load from a secrets manager. |
| `Placement` | `ApiKeyPlacement` | | `Header` | Where the key is delivered: `Header`, `BearerToken`, or `QueryParameter`. |
| `KeyName` | `string` | | `"X-API-Key"` | The header name (for `Header` placement) or query parameter name (for `QueryParameter`). Ignored for `BearerToken`. |
| `HeaderPrefix` | `string` | | `""` | Prefix prepended to the key value when using `Header` placement. E.g. `"ApiKey "` produces `X-API-Key: ApiKey secret`. |

---

## Subject claim

`Subject` is set to `KeyName`. This becomes the JWT `sub` claim — it identifies which key/integration authenticated, not a person.

---

## Security considerations

**Store in a secrets manager.** An API key is as sensitive as a password. Store it in Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, or `dotnet user-secrets` — never commit it to source control.

**Prefer Header over QueryParameter.** Query parameters are routinely captured in: server access logs, CDN edge logs, browser history, referrer headers sent to third-party resources, and error reports. A key in a header is invisible to all of these.

**Rotate regularly.** Unlike JWTs which expire on their own, an API key is valid until it is revoked. Build a rotation process — at minimum, rotate on any suspected leak.

**HTTPS is required.** An API key in a header is plaintext over the wire. Always use TLS.

---

## Strategy name

```
"ApiKey"   (or whatever explicit name you passed to .AddApiKey("name", o => …))
```

---

## Registration

```csharp
services.AddAuthentication()
    .AddApiKey(o =>
    {
        o.ApiKey    = configuration["ApiKey:Secret"]!;
        o.Placement = ApiKeyPlacement.Header;  // default
        o.KeyName   = "X-API-Key";             // default
    });
```

---

## Placement modes

### `Header` (default)

The key is placed in a custom request header.

```
X-API-Key: your-secret-key
```

With an optional prefix:

```csharp
o.HeaderPrefix = "ApiKey ";
// → X-API-Key: ApiKey your-secret-key
```

### `BearerToken`

The key is returned as a standard Bearer token.

```
Authorization: Bearer your-secret-key
```

```csharp
o.Placement = ApiKeyPlacement.BearerToken;
```

### `QueryParameter`

The key is returned as the `AccessToken` value; the caller appends it to the URL.

```
https://api.example.com/resource?X-API-Key=your-secret-key
```

```csharp
o.Placement = ApiKeyPlacement.QueryParameter;
o.KeyName   = "api_key"; // controls the query parameter name
```

> The caller is responsible for appending `?{KeyName}={AccessToken}` to the request URL.
> The strategy only resolves the value — it does not construct the URL.

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `ApiKey` | `string` | ✅ | — | The secret key value |
| `Placement` | `ApiKeyPlacement` | | `Header` | Where the key is delivered |
| `KeyName` | `string` | | `"X-API-Key"` | Header name or query parameter name |
| `HeaderPrefix` | `string` | | `""` | Prefix prepended to the key in `Header` placement |

---

## Subject claim

The `Subject` is set to `KeyName` (the name of the header or parameter carrying the key).

---

## Security considerations

- Store the API key in a secrets manager; never commit it to source control.
- Prefer `BearerToken` or `Header` placement over `QueryParameter` — query parameters
  are logged by proxies, CDNs, and server access logs.
- Always use HTTPS to prevent the key from being intercepted in transit.

---

## Can handle check

`CanHandleAsync()` returns `false` when `ApiKey` is null or whitespace.

---

## Strategy name

```
"ApiKey"
```