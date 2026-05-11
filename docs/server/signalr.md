---
layout: default
title: SignalR Hub Filter
description: AuthenticationHubFilter validates JWT tokens on connection and before every hub method invocation.
permalink: /server/signalr/
---

## Why SignalR authentication is different

SignalR uses persistent connections — WebSockets in most cases, with long-polling as a fallback.
This creates authentication challenges that don't exist in regular HTTP:

**Browsers cannot set headers on WebSocket connections.** The `Authorization: Bearer <token>` header
that works perfectly for `fetch()` calls is not available during the WebSocket handshake. The
browser WebSocket API does not expose a way to set custom headers. This means the token must be
transmitted some other way — the accepted workaround is to include it as a URL query parameter
(`?access_token=...`). The server reads it from the URL rather than the header.

**Sessions outlive tokens.** A client connects once and holds the connection open for minutes or
hours. An access token issued at connection time might expire while the session is still active.
A guard at connection time alone is not sufficient — the token must also be validated before each
hub method call.

`AuthenticationHubFilter` handles both concerns: it validates at connection and re-validates before
every method invocation, accepting the token from either the `Authorization` header (for .NET
clients) or the `?access_token=` query string (for browser clients).

---

## Overview

`AuthenticationHubFilter` implements `IHubFilter` and guards SignalR hubs at two points:

1. **`OnConnectedAsync`** — validates the token when the client connects; disconnects unauthenticated clients immediately
2. **`InvokeMethodAsync`** — re-validates the token before each hub method call (guards against tokens that expire mid-session)

The filter accepts the token from two sources (checked in order):

| Source | Format |
|---|---|
| `Authorization` header | `Bearer <token>` or raw token |
| `?access_token=` query parameter | Raw token (required by browser SignalR clients) |

---

## Registration

```csharp
builder.Services
    .AddAuthentication()
    .AddJwtTokenIssuance(o => { … });

builder.Services.AddPrimitivesAspNetCoreAuthentication();

builder.Services.AddSignalR(o =>
{
    o.AddFilter<AuthenticationHubFilter>();
});
```

To apply the filter to a specific hub only:

```csharp
builder.Services.AddSignalR();
builder.Services.AddSingleton<AuthenticationHubFilter>();

// In the hub class, apply the filter attribute:
[HubMethodName("…")]   // standard approach — per-hub filters require manual registration
```

---

## Accessing the authenticated user

After the filter passes, `Context.User` is populated with the JWT claims:

```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        var sender = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? "anonymous";

        await Clients.All.SendAsync("ReceiveMessage", sender, message);
    }
}
```

---

## JavaScript client — sending the token

Browser SignalR clients cannot set arbitrary headers. Use the `accessToken` factory function:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub", {
        accessTokenFactory: () => getAccessToken()  // return the JWT string
    })
    .build();

await connection.start();
```

The token is sent as `?access_token=<token>` and the hub filter reads it from the query string automatically.

---

## .NET client — sending the token

Use the Primitives SignalR extension (see [SignalR Client]({{ '/client/signalr/' | relative_url }})):

```csharp
var connection = new HubConnectionBuilder()
    .WithPrimitivesAuthentication(
        hubUrl:       "https://my-hub.example.com/chathub",
        tokenService: tokenService,
        strategyName: "OIDC")
    .Build();

await connection.StartAsync();
```

This re-acquires a fresh token on every reconnect automatically.
