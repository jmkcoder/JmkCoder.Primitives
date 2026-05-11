---
layout: default
library: authentication
title: gRPC Client
description: PrimitivesGrpcCredentials and AuthenticatingClientInterceptor inject Bearer tokens into every outbound gRPC call.
permalink: /authentication/client/grpc/
---

## gRPC client credentials vs HTTP headers

When you make an HTTP call, setting an `Authorization` header is trivial — it’s just a header.
gRPC has two different mechanisms, and which one you use depends on the channel security:

**`CallCredentials`** — the gRPC-native approach. Credentials are provided as a delegate that
attaches metadata before each call. This is the correct approach for production TLS channels.
However, gRPC enforces that `CallCredentials` can only be used with encrypted channels (TLS).
Attempting to use them on a plaintext (`http://`) channel throws a `InvalidOperationException`.

**Interceptor** — an alternative that works with plaintext channels. The interceptor pattern
applies metadata in the same way as `AuthenticatingHandler` does for HTTP — it runs before
every outbound call and injects the token. Use this in development environments, service-mesh
scenarios where TLS is terminated at the sidecar, or any time you cannot use `CallCredentials`.

`PrimitivesGrpcCredentials` provides both:
- `.Create(…)` — for TLS channels (production)
- `.CreateInterceptor(…)` — for insecure channels (development)

---

## Overview

Two options depending on the channel security:

| Scenario | API |
|---|---|
| HTTPS / TLS channel | `PrimitivesGrpcCredentials.Create(…)` — uses `CallCredentials` |
| HTTP / insecure channel | `PrimitivesGrpcCredentials.CreateInterceptor(…)` — uses an `Interceptor` |

Both inject a fresh `Bearer` token into the call metadata for **all gRPC call types**: unary, client-streaming, server-streaming, and bidirectional streaming.

---

## TLS channel (production)

```csharp
// Resolve ITokenIssuanceService from DI
var tokenService = sp.GetRequiredService<ITokenIssuanceService>();

var credentials = PrimitivesGrpcCredentials.Create(
    tokenService,
    strategyName: "OIDC");

var channel = GrpcChannel.ForAddress("https://my-grpc-service.example.com",
    new GrpcChannelOptions { Credentials = credentials });

var client = new OrderService.OrderServiceClient(channel);
```

<div class="bd-callout bd-callout-danger">
<strong>TLS required for CallCredentials.</strong> gRPC <code>CallCredentials</code> only work with
TLS-secured channels. For plaintext channels (development, service-mesh mTLS), use the interceptor approach below.
</div>

---

## Insecure / HTTP channel (development)

```csharp
var interceptor = PrimitivesGrpcCredentials.CreateInterceptor(
    tokenService,
    strategyName: "OIDC");

var channel = GrpcChannel.ForAddress("http://localhost:5000");
var invoker  = channel.Intercept(interceptor);

var client = new OrderService.OrderServiceClient(invoker);
```

---

## DI registration pattern

Register as a singleton and inject via the typed client:

```csharp
// In Program.cs
builder.Services.AddSingleton(sp =>
{
    var tokenService = sp.GetRequiredService<ITokenIssuanceService>();
    var creds = PrimitivesGrpcCredentials.Create(tokenService, "OIDC");

    return GrpcChannel.ForAddress("https://my-grpc-service.example.com",
        new GrpcChannelOptions { Credentials = creds });
});

builder.Services.AddTransient(sp =>
    new OrderService.OrderServiceClient(sp.GetRequiredService<GrpcChannel>()));
```

---

## Token refresh

`AuthenticatingClientInterceptor` acquires a new token before each call via `ITokenIssuanceService.AuthenticateAsync`. The `ITokenIssuanceService` uses its internal cache (see [Caching]({{ '/authentication/caching/' | relative_url }})), so most calls hit the cache rather than re-authenticating. Configure `EarlyExpiryBuffer` to control how early the cache is invalidated.

---

## `AuthenticatingClientInterceptor` call types

| gRPC call type | Method |
|---|---|
| Unary | `AsyncUnaryCall` + `BlockingUnaryCall` |
| Client streaming | `AsyncClientStreamingCall` |
| Server streaming | `AsyncServerStreamingCall` |
| Bidirectional streaming | `AsyncDuplexStreamingCall` |