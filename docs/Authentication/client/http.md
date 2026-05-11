---
layout: default
library: authentication
title: HTTP / HttpClient
description: AuthenticatingHandler is a DelegatingHandler that acquires tokens, attaches them as Authorization headers, and retries on 401.
permalink: /authentication/client/http/
---

## Why use AuthenticatingHandler?

Without `AuthenticatingHandler`, every place in your code that calls a protected API needs to:

1. Call `ITokenIssuanceService.AuthenticateAsync()` to get a token
2. Check whether the token is still valid (or just cached and about to expire)
3. Set `Authorization: Bearer <token>` on the `HttpRequestMessage`
4. Handle `401 Unauthorized` responses by refreshing the token and retrying
5. Coordinate concurrent refresh attempts so the token endpoint isnΓÇÖt hammered in parallel

`AuthenticatingHandler` does all five automatically. It slots into the `HttpClient` pipeline as a
`DelegatingHandler`, so from the perspective of any code that injects an `HttpClient`, the token
management is completely invisible. You write code that calls APIs; the handler handles auth.

---

## How it works

`AuthenticatingHandler` wraps any `HttpClient` and:

1. **Before every request** ΓÇö acquires a token via `ITokenIssuanceService.AuthenticateAsync` and attaches `Authorization: Bearer <token>`
2. **On a `401 Unauthorized` response** ΓÇö tries to refresh the token (if a refresh token is cached), then retries the request once
3. **Refresh fallback** ΓÇö if refresh fails, falls back to full re-authentication and retries
4. **Streaming bodies** ΓÇö if the request body is a `StreamContent` (not bufferable), the 401 is returned as-is without retry

**Thundering herd prevention:** When many requests fire simultaneously and the token has just expired, only one of them will attempt a refresh. The others wait (via `SemaphoreSlim`) and receive the same new token once it is available. This prevents flooding the token endpoint with redundant refresh calls.

---

## Registration ΓÇö named client (recommended)

```csharp
builder.Services
    .AddAuthentication()
    .AddApiKey("PartnerApi", o => { o.ApiKey = config["PartnerApi:Key"]!; })
    .AddJwtTokenIssuance(o => { ΓÇª });

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

## Registration ΓÇö typed client

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
    strategyName: "OIDC",           // required ΓÇö strategy to use for token acquisition
    tokenPrefix:  "Bearer",         // optional ΓÇö default: "Bearer"
    headerName:   "Authorization")  // optional ΓÇö default: "Authorization"
```

For APIs that expect a different header (e.g. `X-API-Key`):

```csharp
.AddPrimitivesAuthentication(
    strategyName: "ApiKey",
    tokenPrefix:  "",               // no prefix ΓÇö send the raw key
    headerName:   "X-API-Key")
```

---

## 401-retry behaviour

| Body type | Retryable? | Notes |
|---|---|---|
| No body (`GET`, `DELETE`) | Γ£à Yes | Always retried |
| `ByteArrayContent` | Γ£à Yes | Buffered ΓÇö can be re-sent |
| `StringContent` | Γ£à Yes | Buffered ΓÇö can be re-sent |
| `FormUrlEncodedContent` | Γ£à Yes | Buffered ΓÇö can be re-sent |
| `StreamContent` | Γ¥î No | Stream already consumed; 401 returned as-is |
| `MultipartFormDataContent` | Γ¥î No | May contain streams; not retried |

A warning is logged when a non-retryable body receives a 401.

---

## Thread safety

`AuthenticatingHandler` uses `SemaphoreSlim(1,1)` to ensure only one concurrent refresh attempt per handler instance. Subsequent requests wait for the refresh to complete, then re-use the new token ΓÇö avoiding a thundering herd of re-authentication requests.

---

## `AuthenticatingHandlerOptions`

| Property | Default | Description |
|---|---|---|
| `StrategyName` | *(required)* | Name of the strategy to authenticate with |
| `HeaderName` | `"Authorization"` | HTTP header to write the token into |
| `TokenPrefix` | `"Bearer"` | Prefix prepended to the token value in the header |