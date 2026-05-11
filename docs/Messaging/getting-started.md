---
layout: default
library: messaging
title: Installation
description: Add Primitives.Messaging to your .NET 8 project and choose the right broker provider.
permalink: /messaging/getting-started/
---

## Requirements

- .NET 8 or later
- For RabbitMQ: a reachable RabbitMQ 3.11+ node (or cluster)

## Install the packages

Add the core package to every project that needs to publish or consume messages:

```bash
dotnet add package Primitives.Messaging
```

If you are using RabbitMQ as the broker:

```bash
dotnet add package Primitives.Messaging.RabbitMQ
```

## Register the provider

Choose **one** registration method depending on your infrastructure.

### In-memory (development / tests)

Routes messages directly to registered handlers in the same process. No broker required.

```csharp
builder.Services.AddPrimitivesMessaging(options =>
{
    options.DefaultExchange  = string.Empty;
    options.MaxRetryAttempts = 3;
});
```

### RabbitMQ (production)

Requires `Primitives.Messaging.RabbitMQ`.

```csharp
builder.Services.AddPrimitivesMessagingRabbitMQ(
    configureRabbit: o =>
    {
        o.HostName    = builder.Configuration["RabbitMQ:Host"];
        o.UserName    = builder.Configuration["RabbitMQ:User"];
        o.Password    = builder.Configuration["RabbitMQ:Password"];
        o.VirtualHost = "/";
    },
    configureMessaging: o =>
    {
        o.DefaultExchange    = "myapp";
        o.MaxRetryAttempts   = 3;
        o.DeadLetterExchange = "myapp.dead-letter";
    });
```

## Register handlers

Call `AddMessageHandler<THandler, TMessage>()` for every message type you want to consume.
This works the same regardless of which broker provider is registered.

```csharp
builder.Services.AddMessageHandler<OrderCreatedHandler, OrderCreated>(
    queueName:  "orders",
    exchange:   "myapp",
    routingKey: "order.created");

builder.Services.AddMessageHandler<PaymentProcessedHandler, PaymentProcessed>(
    queueName:  "payments",
    exchange:   "myapp",
    routingKey: "payment.processed");
```

## Next steps

- [Publishing messages]({{ '/messaging/publishing/' | relative_url }}) — `IMessagePublisher` and `PublishOptions`
- [Consuming messages]({{ '/messaging/consuming/' | relative_url }}) — implementing `IMessageHandler<T>`
- [Transactional outbox]({{ '/messaging/outbox/' | relative_url }}) — guaranteed delivery with `IOutboxStore`
- [RabbitMQ provider]({{ '/messaging/rabbitmq/' | relative_url }}) — advanced RabbitMQ configuration
