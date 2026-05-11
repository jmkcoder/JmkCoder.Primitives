---
layout: default
library: messaging
title: Consuming Messages
description: Implement IMessageHandler<T> to consume messages. Return Ack, Nack, or Requeue to control delivery semantics.
permalink: /messaging/consuming/
---

## Implement `IMessageHandler<T>`

Create a class that implements `IMessageHandler<T>` for the message type you want to handle:

```csharp
public sealed class OrderCreatedHandler : IMessageHandler<OrderCreated>
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(IOrderRepository orders, ILogger<OrderCreatedHandler> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    public async Task<ConsumeResult> HandleAsync(
        MessageContext<OrderCreated> context,
        CancellationToken cancellationToken)
    {
        try
        {
            await _orders.ProcessAsync(context.Message.OrderId, cancellationToken);
            return ConsumeResult.Ack;
        }
        catch (TransientException ex)
        {
            _logger.LogWarning(ex, "Transient error — requeueing {MessageId}.", context.Metadata.MessageId);
            return ConsumeResult.Requeue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permanent failure — dead-lettering {MessageId}.", context.Metadata.MessageId);
            return ConsumeResult.Nack;
        }
    }
}
```

---

## Register the handler

```csharp
builder.Services.AddMessageHandler<OrderCreatedHandler, OrderCreated>(
    queueName:  "orders",
    exchange:   "myapp",
    routingKey: "order.created");
```

Parameters:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `queueName` | Message type name, lowercase | Queue (or subscription topic) to consume from. |
| `exchange` | `MessagingOptions.DefaultExchange` | Exchange to bind the queue to. |
| `routingKey` | Message type name, lowercase | Routing key pattern for the queue binding. |
| `options` | `new SubscriptionOptions()` | Prefetch count and other subscription settings. |

---

## `ConsumeResult`

| Value | Behaviour |
|-------|-----------|
| `Ack` | Message processed. Acknowledge and remove from queue. |
| `Nack` | Permanent failure. Forward to the dead-letter exchange. |
| `Requeue` | Transient failure. Return to the queue for redelivery. |

<div class="bd-callout bd-callout-warning">
<strong>Avoid infinite requeue loops.</strong> A handler that always returns
<code>Requeue</code> will cycle a message indefinitely. Use the
<code>context.Metadata.DeliveryCount</code> property to detect redeliveries and switch to
<code>Nack</code> after a threshold.
</div>

---

## `MessageContext<T>`

The context object passed to every handler:

```csharp
context.Message                   // the deserialized payload (T)
context.Metadata.MessageId        // unique ID assigned by the publisher
context.Metadata.CorrelationId    // tracing correlation ID (nullable)
context.Metadata.Timestamp        // UTC publish time
context.Metadata.DeliveryCount    // 1 = first delivery; >1 = redelivery
context.Metadata.Headers          // application-level headers
```

---

## Multiple handlers for the same message type

Registering multiple handlers for the same `T` is supported. The in-process publisher and all
broker-specific consumer services invoke them in registration order. If any handler returns
`Nack` or `Requeue`, processing stops and the message is not passed to subsequent handlers.

```csharp
builder.Services.AddMessageHandler<AuditHandler,         OrderCreated>("orders", "myapp", "order.created");
builder.Services.AddMessageHandler<InventoryHandler,     OrderCreated>("orders", "myapp", "order.created");
builder.Services.AddMessageHandler<NotificationHandler,  OrderCreated>("orders", "myapp", "order.created");
```
