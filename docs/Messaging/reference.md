---
layout: default
library: messaging
title: Configuration Reference
description: Complete reference for MessagingOptions, RabbitMQOptions, PublishOptions, and SubscriptionOptions.
permalink: /messaging/reference/
---

## `MessagingOptions`

Global defaults applied to every publish and consumer unless overridden.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultExchange` | `string` | `""` | Exchange used when `PublishOptions.Exchange` is not set. Empty string uses the broker default exchange. |
| `MaxRetryAttempts` | `int` | `3` | Maximum delivery attempts before a message is considered permanently failed. |
| `RetryDelay` | `TimeSpan` | `00:00:05` | Delay between retry attempts. |
| `DeadLetterExchange` | `string` | `"dead-letter"` | Exchange messages are forwarded to after all retry attempts are exhausted. |

### Example

```csharp
builder.Services.AddPrimitivesMessaging(options =>
{
    options.DefaultExchange    = "myapp";
    options.MaxRetryAttempts   = 5;
    options.RetryDelay         = TimeSpan.FromSeconds(10);
    options.DeadLetterExchange = "myapp.dead-letter";
});
```

---

## `PublishOptions`

Per-message options passed to `IMessagePublisher.PublishAsync<T>`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Exchange` | `string?` | `null` | Override the target exchange for this message. |
| `RoutingKey` | `string?` | `null` | Override the routing key. Defaults to the message type name in lowercase. |
| `CorrelationId` | `string?` | `null` | Correlation ID for distributed tracing. |
| `Ttl` | `TimeSpan?` | `null` | Message expires after this duration if not consumed. |
| `Headers` | `IDictionary<string, string>` | `{}` | Additional application-level headers. |

---

## `SubscriptionOptions`

Per-handler options passed to `AddMessageHandler<THandler, TMessage>`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PrefetchCount` | `ushort` | `10` | Maximum unacknowledged messages the broker delivers before waiting for acknowledgements. |

### Example

```csharp
builder.Services.AddMessageHandler<OrderCreatedHandler, OrderCreated>(
    queueName: "orders",
    exchange:  "myapp",
    options:   new SubscriptionOptions { PrefetchCount = 20 });
```

---

## `RabbitMQOptions`

`Primitives.Messaging.RabbitMQ` only.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HostName` | `string` | `"localhost"` | RabbitMQ server hostname. |
| `Port` | `int` | `5672` | AMQP port. |
| `UserName` | `string` | `"guest"` | Broker username. |
| `Password` | `string` | `"guest"` | Broker password. |
| `VirtualHost` | `string` | `"/"` | Virtual host. |
| `ClientProvidedName` | `string?` | `null` | Name shown in the RabbitMQ management UI. |
| `ConnectionFactoryFactory` | `Func<IServiceProvider, IConnectionFactory>?` | `null` | Custom factory override. When set, all other connection properties are ignored. |
| `DeclareTopology` | `bool` | `true` | Declare exchanges, queues, and bindings on consumer startup. Set to `false` when topology is managed externally. |
| `ExchangeType` | `string` | `"topic"` | Exchange type for auto-declared exchanges (`direct`, `fanout`, `topic`, `headers`). |

---

## `ConsumeResult`

Returned by every `IMessageHandler<T>.HandleAsync` implementation.

| Value | Meaning |
|-------|---------|
| `Ack` | Message processed successfully. Acknowledged and removed from the queue. |
| `Nack` | Permanent failure. Message forwarded to the dead-letter exchange. |
| `Requeue` | Transient failure. Message returned to the queue for redelivery. |
