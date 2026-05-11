---
layout: default
title: SignalR Hub Filter
description: AuthenticationHubFilter validates JWT tokens on connection and before every hub method invocation.
permalink: /server/signalr/
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
