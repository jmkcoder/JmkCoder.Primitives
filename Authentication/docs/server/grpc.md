---
layout: default
title: gRPC Interceptor
description: AuthenticationServerInterceptor validates Bearer tokens on every inbound gRPC call type — unary, streaming, and bidirectional.
permalink: /server/grpc/
---

## gRPC metadata vs HTTP headers

gRPC is based on HTTP/2, but it doesn’t use the familiar HTTP `Authorization` header in the way
you might expect. Instead, it uses **metadata** — key-value pairs attached to each call in the
HTTP/2 HEADERS frame. The field is still named `authorization` (lowercase), but the mechanism
for reading and writing it is gRPC-specific.

On the server side, metadata is accessed via `ServerCallContext.RequestHeaders`. On the client
side, it is attached via `CallOptions.Headers` or through `CallCredentials`. This is why gRPC
authentication requires dedicated handling that is separate from the `UseAuthentication()` ASP.NET
Core middleware — the middleware reads `HttpContext.Request.Headers`, which is a different
abstraction.

`AuthenticationServerInterceptor` bridges the gap: it runs in the gRPC interceptor pipeline
(which runs before your service method), reads the metadata, validates the JWT, and populates
`ServerCallContext` so your service methods can access the authenticated `ClaimsPrincipal`.

---

## Overview

`AuthenticationServerInterceptor` is a `Grpc.Core.Interceptors.Interceptor` subclass that:

1. Reads the `authorization` metadata key from the inbound call context (case-insensitive)
2. Strips the `Bearer ` prefix if present — raw tokens are also accepted
3. Calls `IJwtTokenValidator.ValidateAsync(token)`
4. On success, the call continues with a populated `ServerCallContext`
5. On failure, throws `RpcException(StatusCode.Unauthenticated)`

The interceptor handles **all four gRPC call types**: unary, server-streaming, client-streaming, and bidirectional streaming.

---

## Registration

`AddPrimitivesAspNetCoreAuthentication()` registers the interceptor as a singleton. Add it to the gRPC pipeline:

```csharp
builder.Services
    .AddAuthentication()
    .AddJwtTokenIssuance(o => { … });

builder.Services.AddPrimitivesAspNetCoreAuthentication();

builder.Services.AddGrpc(o =>
{
    o.Interceptors.Add<AuthenticationServerInterceptor>();
});
```

To apply the interceptor only to specific services:

```csharp
builder.Services.AddGrpc();
builder.Services.AddGrpcServiceOptions<MyService.MyServiceBase>(o =>
{
    o.Interceptors.Add<AuthenticationServerInterceptor>();
});
```

---

## Sending the token from a client

The interceptor reads the standard `authorization` metadata key. Most gRPC clients set it via call credentials or metadata:

**Using Primitives client (recommended):**

```csharp
// TLS channel
var creds   = PrimitivesGrpcCredentials.Create(tokenService, "OIDC");
var channel = GrpcChannel.ForAddress("https://my-service:5001",
    new GrpcChannelOptions { Credentials = creds });
```

**Manual metadata:**

```csharp
var headers = new Metadata
{
    { "authorization", $"Bearer {accessToken}" }
};
var response = await client.MyMethodAsync(request, headers);
```

---

## Error handling

When a token is missing or invalid, the interceptor throws:

```csharp
throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
```

On the client side, catch `RpcException`:

```csharp
try
{
    var response = await client.MyMethodAsync(request);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
{
    // token expired — re-authenticate and retry
}
```

<div class="bd-callout bd-callout-tip">
<strong>Tip:</strong> Use the client-side <code>AuthenticatingClientInterceptor</code> or
<code>PrimitivesGrpcCredentials</code> — they handle token refresh and retry automatically.
See <a href="{{ '/client/grpc/' | relative_url }}">gRPC Client</a>.
</div>
