---
layout: default
library: messaging
title: RabbitMQ Provider
description: Configure Primitives.Messaging.RabbitMQ for production message delivery with publisher confirms, dead-letter routing, and automatic topology declaration.
permalink: /messaging/rabbitmq/
---

## Install

```bash
dotnet add package Primitives.Messaging.RabbitMQ
```

## Register

```csharp
builder.Services.AddPrimitivesMessagingRabbitMQ(
    configureRabbit: o =>
    {
        o.HostName            = "localhost";
        o.Port                = 5672;
        o.UserName            = "guest";
        o.Password            = "guest";
        o.VirtualHost         = "/";
        o.ClientProvidedName  = "myapp-publisher";
        o.DeclareTopology     = true;
        o.ExchangeType        = "topic";
    },
    configureMessaging: o =>
    {
        o.DefaultExchange    = "myapp";
        o.MaxRetryAttempts   = 3;
        o.DeadLetterExchange = "myapp.dead-letter";
    });
```

---

## Publisher confirms

`RabbitMQMessagePublisher` calls `IModel.ConfirmSelect()` on the channel and
`WaitForConfirmsOrDie(10s)` after every `BasicPublish`. This gives at-least-once delivery
guarantees — the publish call will not return until the broker has persisted the message.

Messages are marked as `DeliveryMode = 2` (persistent) so they survive a broker restart.

---

## Automatic topology declaration

When `RabbitMQOptions.DeclareTopology = true` (the default), the consumer service declares:

1. The exchange (durable, not auto-delete)
2. The queue with `x-dead-letter-exchange` set to `MessagingOptions.DeadLetterExchange`
3. A binding from the queue to the exchange using the configured routing key

Set `DeclareTopology = false` when exchanges and queues are managed externally via
infrastructure-as-code (Terraform, Pulumi) or the RabbitMQ management plugin.

---

## Dead-letter routing

Messages that handlers return `ConsumeResult.Nack` for are forwarded to the dead-letter exchange
configured in `MessagingOptions.DeadLetterExchange` (default: `"dead-letter"`). Declare the DLX
and its queues separately to inspect or replay failed messages.

---

## Reuse an existing connection

If your application already holds an `IConnection` (e.g. for pub/sub or custom topology), pass it
through `ConnectionFactoryFactory`:

```csharp
// Register your connection once
builder.Services.AddSingleton<IConnection>(_ =>
    new ConnectionFactory { HostName = "localhost" }.CreateConnection());

// Tell the provider to reuse it
builder.Services.AddPrimitivesMessagingRabbitMQ(o =>
{
    o.ConnectionFactoryFactory = sp =>
    {
        // Return a factory that wraps the existing connection
        // Note: IConnectionFactory.CreateConnection() is called once by the provider
        throw new InvalidOperationException("Use the pre-registered IConnection singleton.");
    };
});
```

Alternatively, register `IConnection` yourself before calling `AddPrimitivesMessagingRabbitMQ`:
`TryAddSingleton` is used internally, so an already-registered `IConnection` takes precedence.

```csharp
builder.Services.AddSingleton<IConnection>(
    _ => new ConnectionFactory { HostName = "rabbitmq" }.CreateConnection());

builder.Services.AddPrimitivesMessagingRabbitMQ(o => { /* only exchange/queue config needed */ });
```

---

## RabbitMQ cluster

No special configuration is needed. Provide a comma-separated endpoint list in a custom
`ConnectionFactory`:

```csharp
builder.Services.AddPrimitivesMessagingRabbitMQ(o =>
{
    o.ConnectionFactoryFactory = _ =>
        new ConnectionFactory
        {
            HostName = "rabbitmq-node1",
        };
    // Or use AmqpTcpEndpoint list for proper cluster configuration
});
```

---

## Docker Compose (local dev)

```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"  # management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```
