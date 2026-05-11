---
layout: default
title: Client Overview
description: The Client package attaches tokens to outbound HTTP, gRPC, SignalR, and message queue requests — automatically handling refresh on 401.
permalink: /client/
---

## What is the client package for?

Most services are both _consumers_ and _providers_ of protected APIs. Your service issues tokens
for its own clients (using the core or AspNetCore package), and it also needs to _attach_ tokens
to outbound requests when calling other protected APIs.

Managing tokens for outbound calls manually is error-prone:
- Tokens expire — you need to refresh before every call or handle 401s mid-flight
- Multiple concurrent requests can race to refresh the same token — causing thundering herd
- Every transport (HTTP, gRPC, SignalR, message queues) has a different mechanism for attaching credentials

`Primitives.Authentication.Client` handles all of this. You register the transport adapter once at
startup; the adapter acquires, caches, refreshes, and attaches tokens transparently.

---

## Package

```bash
dotnet add package Primitives.Authentication.Client
```

---

## Registration

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … });

// Registers IMessageTokenAttacher (for MQ producers)
builder.Services.AddPrimitivesClientAuthentication();
```

<div class="bd-callout bd-callout-danger">
<strong>Order matters.</strong> <code>AddPrimitivesClientAuthentication()</code> resolves <code>ITokenIssuanceService</code>
at first DI resolution. It must be called after <code>AddJwtTokenIssuance()</code>.
</div>

---

## What's in this section

| Page | Transport | Description |
|---|---|---|
| [HTTP / HttpClient]({{ '/client/http/' | relative_url }}) | HTTP | `AuthenticatingHandler` — attaches tokens, retries on 401 |
| [gRPC Client]({{ '/client/grpc/' | relative_url }}) | gRPC | `PrimitivesGrpcCredentials` and `AuthenticatingClientInterceptor` |
| [SignalR Client]({{ '/client/signalr/' | relative_url }}) | WebSocket | `WithPrimitivesAuthentication` hub connection extension |
| [Message Queue]({{ '/client/message-queue/' | relative_url }}) | Any broker | `IMessageTokenAttacher` — writes `Authorization` header into message bags |
