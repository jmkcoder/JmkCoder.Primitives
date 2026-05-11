---
layout: default
library: authentication
title: Message Queue (Client)
description: IMessageTokenAttacher writes Authorization headers into any dictionary-based message header bag — broker-agnostic by design.
permalink: /authentication/client/message-queue/
---

## How message queue token attachment works

When a producer publishes a message to a broker, it needs to prove its identity to the consumer
that will eventually process that message. Unlike HTTP, there is no `Authorization` header in the
HTTP sense — but most message brokers support user-defined **message properties** or **headers**:
key-value pairs attached to each message alongside the payload.

`IMessageTokenAttacher` acquires a JWT and writes it into a plain `IDictionary<string, string>`.
Because it writes to a dictionary, it is completely independent of any specific broker. You create
the dictionary, pass it to the attacher, and then copy the resulting key-value pairs into whatever
your broker uses for message metadata:

```
IDictionary<string, string>  →  { "Authorization": "Bearer eyJ..." }
                    ↓
  RabbitMQ BasicProperties.Headers
  Azure Service Bus ApplicationProperties
  Kafka Message.Headers
  AWS SQS MessageAttributes
```

Internally, `AttachAsync` calls `ITokenIssuanceService.AuthenticateAsync(strategyName)`, extracts
the access token, and writes `headers["Authorization"] = $"Bearer {token}"`.

---

## Overview

`IMessageTokenAttacher` acquires a JWT and writes it into an `IDictionary<string, string>` header bag. Because it works against a plain dictionary, it is completely broker-agnostic — the same code works with RabbitMQ, Azure Service Bus, Kafka, AWS SQS, or any other system that maps message headers to string key-value pairs.

---

## Registration

```csharp
builder.Services
    .AddAuthentication()
    .AddApiKey("PartnerBus", o => { o.ApiKey = config["Bus:ApiKey"]!; })
    .AddJwtTokenIssuance(o => { … });

builder.Services.AddPrimitivesClientAuthentication();  // registers IMessageTokenAttacher
```

---

## Usage

```csharp
public class OrderPublisher(IMessageTokenAttacher tokenAttacher, IBusPublisher bus)
{
    public async Task PublishAsync(Order order, CancellationToken ct)
    {
        var headers = new Dictionary<string, string>();

        var ok = await tokenAttacher.AttachAsync(
            headers,
            strategyName: "PartnerBus",
            cancellationToken: ct);

        if (!ok)
            throw new InvalidOperationException("Could not acquire token for message.");

        // Headers now contains: { "Authorization": "Bearer eyJ..." }
        await bus.PublishAsync(order, headers, ct);
    }
}
```

---

## `IMessageTokenAttacher` interface

```csharp
Task<bool> AttachAsync(
    IDictionary<string, string> headers,
    string                      strategyName,
    CancellationToken           cancellationToken = default);
```

| Parameter | Description |
|---|---|
| `headers` | The message header dictionary to write into. Must not be `null`. |
| `strategyName` | Name of the registered strategy to authenticate with. Must not be empty. |
| Returns `true` | Token was acquired and written to `headers["Authorization"]` |
| Returns `false` | Authentication failed; `headers` is unchanged; a warning is logged |

---

## Header format

The written header is always:

```
Authorization: Bearer eyJhbGci...
```

---

## Consuming on the server

On the receiving end, subclass `MessageAuthenticationMiddlewareBase<TContext>` and implement `IMessageAuthenticationContext.GetToken()` to extract the token from the same header key. See [Message Queue (Server)]({{ '/authentication/server/message-queue/' | relative_url }}) for the full walkthrough.

---

## Custom header name

If your broker uses a different header name, write to it after calling `AttachAsync`:

```csharp
await tokenAttacher.AttachAsync(tempHeaders, "OIDC", ct);
// Move to broker-specific header
myMessage.Headers["X-Auth-Token"] = tempHeaders["Authorization"]["Bearer ".Length..];
```