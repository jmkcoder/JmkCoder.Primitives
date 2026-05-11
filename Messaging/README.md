# Primitives.Messaging

Broker-agnostic message publishing and consumption with retry, dead-letter, and transactional outbox patterns for reliable delivery.

## Packages

| Package | Description |
|---------|-------------|
| `Primitives.Messaging` | Core abstractions, in-memory publisher, and outbox primitives |
| `Primitives.Messaging.RabbitMQ` | RabbitMQ provider with publisher confirms and automatic topology |

## Quick start

```bash
dotnet add package Primitives.Messaging
```

```csharp
// Program.cs
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
    public async Task CompleteAsync(Order order, CancellationToken ct)
    {
        // ... domain logic ...
        await publisher.PublishAsync(new OrderCreated(order.Id), cancellationToken: ct);
    }
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

## RabbitMQ

```bash
dotnet add package Primitives.Messaging.RabbitMQ
```

```csharp
builder.Services.AddPrimitivesMessagingRabbitMQ(
    configureRabbit: o =>
    {
        o.HostName    = "localhost";
        o.UserName    = "guest";
        o.Password    = "guest";
    },
    configureMessaging: o =>
    {
        o.DefaultExchange    = "myapp";
        o.MaxRetryAttempts   = 3;
        o.DeadLetterExchange = "myapp.dead-letter";
    });

builder.Services.AddMessageHandler<OrderCreatedHandler, OrderCreated>(
    queueName:  "orders",
    exchange:   "myapp",
    routingKey: "order.created");
```

## Transactional outbox

```csharp
// Register the in-memory store (dev/test) — replace with a DB-backed store in production
builder.Services.AddInMemoryOutbox();

// Save within your domain transaction
public class OrderService(IOutboxStore outbox)
{
    public async Task PlaceAsync(Order order, CancellationToken ct)
    {
        // ... save order to DB in the same transaction ...

        await outbox.SaveAsync(new OutboxMessage
        {
            MessageType = nameof(OrderCreated),
            Payload     = JsonSerializer.Serialize(new OrderCreated(order.Id)),
            Exchange    = "orders",
            RoutingKey  = "order.created",
        }, ct);
    }
}
```

## License

MIT
