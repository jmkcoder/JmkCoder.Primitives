---
layout: default
title: SignalR Client
description: WithPrimitivesAuthentication injects a fresh JWT into every SignalR hub connection, including automatic token refresh on reconnect.
permalink: /client/signalr/
---

## How SignalR client authentication works

SignalR’s `HubConnectionBuilder` supports an `accessTokenProvider` option — a `Func<Task<string?>>`
that the client calls to get a token before establishing a connection. This function is also called
on every automatic reconnect, ensuring that an expired token from the original connection doesn’t
block reconnection.

`WithPrimitivesAuthentication` wraps `ITokenIssuanceService.AuthenticateAsync()` as the
`accessTokenProvider`, so every connection and reconnection gets a fresh (or recently cached) token
without any manual code.

**What about token expiry mid-session?** The `accessTokenProvider` is called at connection time, not
before every message. If the token expires during a long session, the server-side `AuthenticationHubFilter`
will reject the next hub method call. The connection will close, triggering a reconnect, which will
call `accessTokenProvider` again to get a fresh token. This is the expected and designed behaviour.

---

## Overview

`SignalRHubConnectionExtensions` provides two ways to attach authentication:

| Method | Use when |
|---|---|
| `UsePrimitivesAuthentication` | You're already building `HttpConnectionOptions` manually |
| `WithPrimitivesAuthentication` | You want a single-call builder pattern |

Both inject a fresh token via `accessTokenProvider` — SignalR's built-in hook that is called on every connection (including reconnects), ensuring tokens never expire mid-session.

---

## Option A — fluent builder (recommended)

```csharp
var connection = new HubConnectionBuilder()
    .WithPrimitivesAuthentication(
        hubUrl:       "https://my-hub.example.com/chathub",
        tokenService: tokenService,
        strategyName: "OIDC")
    .Build();

await connection.StartAsync();
```

---

## Option B — configure options manually

Use this when you need to set other `HttpConnectionOptions` as well (e.g. custom headers, transports):

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://my-hub.example.com/chathub", options =>
    {
        options.UsePrimitivesAuthentication(tokenService, strategyName: "OIDC");

        // Other options still work
        options.Transports = HttpTransportType.WebSockets;
    })
    .Build();
```

---

## DI registration

```csharp
// Register the services
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … });

builder.Services.AddPrimitivesClientAuthentication();

// Register the connection as a singleton
builder.Services.AddSingleton(sp =>
{
    var tokenService = sp.GetRequiredService<ITokenIssuanceService>();

    return new HubConnectionBuilder()
        .WithPrimitivesAuthentication(
            hubUrl:       "https://my-hub.example.com/chathub",
            tokenService: tokenService,
            strategyName: "OIDC")
        .Build();
});
```

---

## Reconnect handling

SignalR calls `accessTokenProvider` on every reconnect attempt, so you get a fresh token automatically after a network interruption — no extra code required.

To enable automatic reconnect:

```csharp
var connection = new HubConnectionBuilder()
    .WithPrimitivesAuthentication(hubUrl, tokenService, "OIDC")
    .WithAutomaticReconnect()   // standard SignalR reconnect policy
    .Build();
```

---

## Server setup

The server hub filter validates the token before every method call. See [SignalR Hub Filter]({{ '/server/signalr/' | relative_url }}) for server-side configuration.
