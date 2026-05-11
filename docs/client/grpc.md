---
layout: default
title: gRPC Client
description: PrimitivesGrpcCredentials and AuthenticatingClientInterceptor inject Bearer tokens into every outbound gRPC call.
permalink: /client/grpc/
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

`AuthenticatingClientInterceptor` acquires a new token before each call via `ITokenIssuanceService.AuthenticateAsync`. The `ITokenIssuanceService` uses its internal cache (see [Caching]({{ '/caching/' | relative_url }})), so most calls hit the cache rather than re-authenticating. Configure `EarlyExpiryBuffer` to control how early the cache is invalidated.

---

## `AuthenticatingClientInterceptor` call types

| gRPC call type | Method |
|---|---|
| Unary | `AsyncUnaryCall` + `BlockingUnaryCall` |
| Client streaming | `AsyncClientStreamingCall` |
| Server streaming | `AsyncServerStreamingCall` |
| Bidirectional streaming | `AsyncDuplexStreamingCall` |
