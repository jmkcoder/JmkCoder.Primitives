---
layout: default
title: Message Queue (Server)
description: MessageAuthenticationMiddlewareBase provides a broker-agnostic base class for validating JWT tokens on inbound messages.
permalink: /server/message-queue/
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
