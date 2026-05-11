---
layout: default
library: messaging
permalink: /messaging/
---

<div class="bd-hero">
  <h1>Primitives.Messaging</h1>
  <p class="lead">
    Broker-agnostic message publishing and consumption with retry, dead-letter, and transactional
    outbox patterns for reliable delivery. One interface — <code>IMessagePublisher</code> — works
    across in-memory, RabbitMQ, and any future broker without changing application code.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Messaging
  </div>
</div>

## The problem it solves

.NET messaging code is typically coupled to a specific broker SDK. Swapping RabbitMQ for Azure
Service Bus means rewriting every publish and consume call. Retry logic, dead-lettering, and
outbox patterns are reimplemented in every project.

`Primitives.Messaging` gives you:

- **One interface** — `IMessagePublisher` — with `PublishAsync<T>` for any message type.
- **Swappable brokers** — in-process (dev/test), RabbitMQ, and future providers, chosen at
  registration time.
- **Handler pattern** — implement `IMessageHandler<T>` and the library wires the consumer loop,
  deserialization, ack/nack, and dead-lettering for you.
- **Transactional outbox** — `IOutboxStore` lets you save messages in the same database transaction
  as your domain state, guaranteeing delivery even if the broker is temporarily unavailable.

## Quick start

```csharp
// Program.cs — in-memory bus (single process / dev)
builder.Services.AddPrimitivesMessaging();

builder.Services.AddMessageHandler<OrderCreatedHandler, OrderCreated>(
    queueName:  "orders",
    exchange:   "orders-exchange",
    routingKey: "order.created");
```

```csharp
// Inject and publish
public class CheckoutService(IMessagePublisher publisher)
{
    public async Task CompleteAsync(Order order, CancellationToken ct) =>
        await publisher.PublishAsync(new OrderCreated(order.Id), cancellationToken: ct);
}
```

```csharp
// Handle
public sealed class OrderCreatedHandler : IMessageHandler<OrderCreated>
{
    public async Task<ConsumeResult> HandleAsync(
        MessageContext<OrderCreated> context,
        CancellationToken cancellationToken)
    {
        // process context.Message
        return ConsumeResult.Ack;
    }
}
```

## Packages

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Messaging</div>
    <p>Core abstractions, in-memory publisher, handler registration, and outbox primitives.</p>
    <div class="install-cmd">dotnet add package Primitives.Messaging</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Messaging.RabbitMQ</div>
    <p>RabbitMQ publisher and consumer service with publisher confirms and automatic topology declaration.</p>
    <div class="install-cmd">dotnet add package Primitives.Messaging.RabbitMQ</div>
  </div>
</div>

## Design principles

**Single interface, multiple brokers.** `IMessagePublisher` is the only publish abstraction your
code depends on. Switching from in-memory to RabbitMQ requires changing one line in `Program.cs`.

**Handlers, not callbacks.** Implement `IMessageHandler<T>` and register it. The library handles
the consumer loop, deserialization, ack/nack, and dead-letter routing automatically.

**Explicit delivery semantics.** Handlers return `ConsumeResult.Ack`, `.Nack`, or `.Requeue` —
making delivery intent clear and visible in code review.

**Reliable-by-default.** The transactional outbox pattern is a first-class primitive, not an
afterthought. Implement `IOutboxStore` for your database and guaranteed-delivery is handled for you.
