---
layout: default
library: messaging
title: Publishing Messages
description: Publish messages with IMessagePublisher. Override exchange, routing key, TTL, and headers per message.
permalink: /messaging/publishing/
---

## `IMessagePublisher`

The single publish interface — inject it into any service:

```csharp
public class CheckoutService(IMessagePublisher publisher)
{
    public async Task CompleteAsync(Order order, CancellationToken ct)
    {
        await publisher.PublishAsync(new OrderCreated(order.Id), cancellationToken: ct);
    }
}
```

`PublishAsync<T>` serialises the message as JSON and routes it according to the global
`MessagingOptions` defaults (exchange, routing key = type name in lowercase).

---

## `PublishOptions`

Override per-message settings when needed:

```csharp
await publisher.PublishAsync(
    new OrderCreated(order.Id),
    options: new PublishOptions
    {
        Exchange       = "orders-exchange",
        RoutingKey     = "order.created.priority",
        CorrelationId  = Activity.Current?.TraceId.ToString(),
        Ttl            = TimeSpan.FromMinutes(10),
        Headers        = { ["source"] = "checkout-service" },
    },
    cancellationToken: ct);
```

| Property | Default | Description |
|----------|---------|-------------|
| `Exchange` | `MessagingOptions.DefaultExchange` | Override the target exchange. |
| `RoutingKey` | Message type name, lowercase | Override the routing key. |
| `CorrelationId` | `null` | Attach a tracing correlation ID to the message. |
| `Ttl` | `null` | Message expires after this duration if not consumed. |
| `Headers` | `{}` | Additional application-level headers. |

---

## Routing key convention

When `PublishOptions.RoutingKey` is not set, the routing key defaults to the CLR type name in
lowercase — e.g. `OrderCreated` → `"ordercreated"`. For topic exchanges it is common to use
dotted names; override `RoutingKey` explicitly in that case:

```csharp
await publisher.PublishAsync(
    new OrderCreated(id),
    new PublishOptions { RoutingKey = "order.created" });
```

---

## Publishing from the outbox

For guaranteed delivery across broker failures, save the message to `IOutboxStore` inside your
database transaction instead of publishing directly. A relay worker publishes stored messages
once the transaction commits.

See [Transactional Outbox]({{ '/messaging/outbox/' | relative_url }}) for details.
