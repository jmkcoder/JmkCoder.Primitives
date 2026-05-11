---
layout: default
title: Client Overview
description: The Client package attaches tokens to outbound HTTP, gRPC, SignalR, and message queue requests — automatically handling refresh on 401.
permalink: /client/
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
