---
layout: default
title: API Key
description: Static secret validation with three placement modes — header, query parameter, or Bearer token.
permalink: /strategies/api-key/
---

## Overview

Returns a configured API key formatted for one of three delivery placements:
custom header, Bearer token, or URL query parameter.

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
