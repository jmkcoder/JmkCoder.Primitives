---
layout: default
title: HTTP / HttpClient
description: AuthenticatingHandler is a DelegatingHandler that acquires tokens, attaches them as Authorization headers, and retries on 401.
permalink: /client/http/
---

## How it works

`AuthenticatingHandler` wraps any `HttpClient` and:

1. **Before every request** — acquires a token via `ITokenIssuanceService.AuthenticateAsync` and attaches `Authorization: Bearer <token>`
2. **On a `401 Unauthorized` response** — tries to refresh the token (if a refresh token is cached), then retries the request once
3. **Refresh fallback** — if refresh fails, falls back to full re-authentication and retries
4. **Streaming bodies** — if the request body is a `StreamContent` (not bufferable), the 401 is returned as-is without retry

---

## Registration — named client (recommended)

```csharp
builder.Services
    .AddAuthentication()
    .AddApiKey("PartnerApi", o => { o.ApiKey = config["PartnerApi:Key"]!; })
    .AddJwtTokenIssuance(o => { … });

builder.Services
    .AddHttpClient("PartnerApiClient", c =>
    {
        c.BaseAddress = new Uri("https://partner-api.example.com");
        c.Timeout     = TimeSpan.FromSeconds(30);
    })
    .AddPrimitivesAuthentication(strategyName: "PartnerApi");
```

```csharp
// Inject IHttpClientFactory and resolve by name
public class PartnerApiClient(IHttpClientFactory factory)
{
    private readonly HttpClient _http = factory.CreateClient("PartnerApiClient");

    public Task<PartnerData[]> GetDataAsync(CancellationToken ct)
        => _http.GetFromJsonAsync<PartnerData[]>("/v1/data", ct)!;
}
```

---

## Registration — typed client

```csharp
builder.Services
    .AddHttpClient<PartnerApiClient>(c =>
    {
        c.BaseAddress = new Uri("https://partner-api.example.com");
    })
    .AddPrimitivesAuthentication(strategyName: "OIDC");
```

---

## `AddPrimitivesAuthentication` options

```csharp
.AddPrimitivesAuthentication(
    strategyName: "OIDC",           // required — strategy to use for token acquisition
    tokenPrefix:  "Bearer",         // optional — default: "Bearer"
    headerName:   "Authorization")  // optional — default: "Authorization"
```

For APIs that expect a different header (e.g. `X-API-Key`):

```csharp
.AddPrimitivesAuthentication(
    strategyName: "ApiKey",
    tokenPrefix:  "",               // no prefix — send the raw key
    headerName:   "X-API-Key")
```

---

## 401-retry behaviour

| Body type | Retryable? | Notes |
|---|---|---|
| No body (`GET`, `DELETE`) | ✅ Yes | Always retried |
| `ByteArrayContent` | ✅ Yes | Buffered — can be re-sent |
| `StringContent` | ✅ Yes | Buffered — can be re-sent |
| `FormUrlEncodedContent` | ✅ Yes | Buffered — can be re-sent |
| `StreamContent` | ❌ No | Stream already consumed; 401 returned as-is |
| `MultipartFormDataContent` | ❌ No | May contain streams; not retried |

A warning is logged when a non-retryable body receives a 401.

---

## Thread safety

`AuthenticatingHandler` uses `SemaphoreSlim(1,1)` to ensure only one concurrent refresh attempt per handler instance. Subsequent requests wait for the refresh to complete, then re-use the new token — avoiding a thundering herd of re-authentication requests.

---

## `AuthenticatingHandlerOptions`

| Property | Default | Description |
|---|---|---|
| `StrategyName` | *(required)* | Name of the strategy to authenticate with |
| `HeaderName` | `"Authorization"` | HTTP header to write the token into |
| `TokenPrefix` | `"Bearer"` | Prefix prepended to the token value in the header |
