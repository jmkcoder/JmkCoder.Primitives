---
layout: default
title: Message Queue (Server)
description: MessageAuthenticationMiddlewareBase provides a broker-agnostic base class for validating JWT tokens on inbound messages.
permalink: /server/message-queue/
---

## Why message queue authentication is different

HTTP is synchronous: a client sends a request and waits for a response. Authentication is
straightforward — a 401 response tells the client to re-authenticate before retrying.

Message queues are asynchronous: a producer publishes a message to a broker (RabbitMQ, Azure
Service Bus, Kafka, SQS) and moves on. The consumer processes the message later — possibly seconds,
minutes, or hours later. By the time your consumer reads the message:

- The producer’s JWT access token may have **already expired**.
- There is no synchronous channel to send a 401 — the message is either processed or dead-lettered.
- The producer cannot be prompted to re-authenticate mid-flight.

This creates a fundamental tension: you want the consumer to verify the message came from an
authorized producer, but the short-lived JWT model doesn’t map naturally to fire-and-forget delivery.

**Recommended approaches:**

1. **Issue a long-lived token** just for message signing (with a dedicated audience claim), or
2. **Sign with an API key** instead of a JWT (no expiry concern), or
3. **Accept expired tokens** with a grace window if the broker’s delivery latency is bounded.

`MessageAuthenticationMiddlewareBase<TContext>` gives you the infrastructure to implement whichever
approach fits your requirements. You provide the `IMessageAuthenticationContext` that knows how to
extract the credential from your broker’s message format; the base class handles validation.

---

## Overview

`MessageAuthenticationMiddlewareBase<TContext>` is an abstract class you subclass to authenticate inbound messages from any broker — RabbitMQ, Azure Service Bus, Kafka, AWS SQS, etc.

Your `TContext` type must implement `IMessageAuthenticationContext`:

```csharp
public interface IMessageAuthenticationContext
{
    string? GetToken();   // return the raw JWT (without "Bearer " prefix)
}
```

---

## Implementing the context

Create a context class that wraps your broker's message type:

```csharp
// RabbitMQ example
public sealed class RabbitMessageContext : IMessageAuthenticationContext
{
    private readonly BasicDeliverEventArgs _args;

    public RabbitMessageContext(BasicDeliverEventArgs args)
        => _args = args;

    public string? GetToken()
    {
        if (_args.BasicProperties.Headers.TryGetValue("Authorization", out var raw)
            && raw is byte[] bytes)
        {
            var header = Encoding.UTF8.GetString(bytes);
            return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..]
                : header;
        }
        return null;
    }
}
```

---

## Implementing the middleware

```csharp
public sealed class RabbitAuthMiddleware
    : MessageAuthenticationMiddlewareBase<RabbitMessageContext>
{
    private readonly IModel _channel;

    public RabbitAuthMiddleware(IJwtTokenValidator validator, IModel channel)
        : base(validator)
    {
        _channel = channel;
    }

    public async Task ProcessAsync(RabbitMessageContext context, Func<Task> next)
    {
        var authenticated = await AuthenticateAsync(context);

        if (!authenticated)
        {
            // Reject / dead-letter
            _channel.BasicNack(context.DeliveryTag, false, false);
            return;
        }

        // context.Principal is now populated
        await next();
    }
}
```

`AuthenticateAsync` calls `IMessageAuthenticationContext.GetToken()`, validates the JWT, and sets `Principal` on the base class.

---

## Registration

```csharp
builder.Services
    .AddAuthentication()
    .AddJwtTokenIssuance(o => { … });

builder.Services.AddPrimitivesAspNetCoreAuthentication();
builder.Services.AddScoped<RabbitAuthMiddleware>();
```

---

## Accessing the principal

`MessageAuthenticationMiddlewareBase<TContext>` exposes:

```csharp
protected ClaimsPrincipal? Principal { get; }
```

Available after `AuthenticateAsync` returns `true`. Cast claims as needed:

```csharp
var subject = Principal!.FindFirstValue(ClaimTypes.NameIdentifier);
```

---

## Producing tokens for messages

On the producer side, use `IMessageTokenAttacher` from the Client package. See [Message Queue (Client)]({{ '/client/message-queue/' | relative_url }}).
