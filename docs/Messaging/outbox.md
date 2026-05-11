---
layout: default
library: messaging
title: Transactional Outbox
description: Guarantee message delivery across broker failures by saving messages in the same database transaction as your domain state.
permalink: /messaging/outbox/
---

## The problem

Publishing to a broker inside a database transaction is not atomic. If the transaction commits but
the broker publish fails (network blip, broker restart), the event is silently lost. Conversely,
if the publish succeeds but the transaction rolls back, a phantom event is delivered.

The transactional outbox pattern solves this by storing the message as a row in the same database
and using a relay worker to forward it to the broker after the transaction commits.

---

## How it works

```
┌──────────────────────────────────────────┐
│  Application transaction                 │
│                                          │
│  1. Save domain entity  ─────────► DB    │
│  2. outbox.SaveAsync()  ─────────► DB    │
│                                          │
└──────────────────────────────────────────┘
          (transaction commits atomically)

┌──────────────────────────────────────────┐
│  Relay worker (background loop)          │
│                                          │
│  3. GetPendingAsync()   ◄───────── DB    │
│  4. publisher.PublishAsync()  ──► Broker │
│  5. MarkPublishedAsync()  ───────► DB    │
│                                          │
└──────────────────────────────────────────┘
```

---

## `IOutboxStore`

```csharp
public interface IOutboxStore
{
    Task SaveAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task MarkPublishedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string reason, CancellationToken ct = default);
}
```

---

## `OutboxMessage`

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Auto-generated unique identifier. |
| `MessageType` | `string` | CLR type name of the payload (for logging/debugging). |
| `Payload` | `string` | JSON-serialized message body. |
| `Exchange` | `string` | Target exchange to publish to. |
| `RoutingKey` | `string` | Routing key to use. |
| `CreatedAt` | `DateTimeOffset` | UTC creation time. |
| `PublishedAt` | `DateTimeOffset?` | Set by the relay when the message is published. |
| `Error` | `string?` | Last error message, if a publish attempt failed. |
| `AttemptCount` | `int` | Number of publish attempts made. |

---

## In-memory store (dev / tests)

Register `InMemoryOutboxStore` for development and testing:

```csharp
builder.Services.AddInMemoryOutbox();
```

Then inject `IOutboxStore`:

```csharp
public class OrderService(IOrderRepository orders, IOutboxStore outbox)
{
    public async Task PlaceAsync(Order order, CancellationToken ct)
    {
        await orders.SaveAsync(order, ct);

        await outbox.SaveAsync(new OutboxMessage
        {
            MessageType = nameof(OrderCreated),
            Payload     = JsonSerializer.Serialize(new OrderCreated(order.Id)),
            Exchange    = "myapp",
            RoutingKey  = "order.created",
        }, ct);
    }
}
```

---

## Production store

Implement `IOutboxStore` against your database. The key constraint is that `SaveAsync` must
participate in the same database transaction as your domain entity save.

**EF Core example (PostgreSQL / SQL Server):**

```csharp
public sealed class EfCoreOutboxStore(AppDbContext db) : IOutboxStore
{
    public async Task SaveAsync(OutboxMessage message, CancellationToken ct)
        => await db.Set<OutboxMessage>().AddAsync(message, ct);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct)
        => await db.Set<OutboxMessage>()
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task MarkPublishedAsync(Guid id, CancellationToken ct)
        => db.Set<OutboxMessage>()
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.PublishedAt, DateTimeOffset.UtcNow), ct);

    public Task MarkFailedAsync(Guid id, string reason, CancellationToken ct)
        => db.Set<OutboxMessage>()
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Error, reason)
                .SetProperty(m => m.AttemptCount, m => m.AttemptCount + 1), ct);
}
```

Register it:

```csharp
builder.Services.AddScoped<IOutboxStore, EfCoreOutboxStore>();
```

---

## Relay worker

The relay polls `IOutboxStore.GetPendingAsync` and publishes each message via `IMessagePublisher`.
Implement it as a `BackgroundService`:

```csharp
public sealed class OutboxRelayWorker(
    IOutboxStore outbox,
    IMessagePublisher publisher,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pending = await outbox.GetPendingAsync(batchSize: 50, stoppingToken);

            foreach (var msg in pending)
            {
                try
                {
                    // Publish the raw JSON payload via a custom wrapper or a raw-bytes overload
                    // In a real implementation, deserialize to the correct type using msg.MessageType
                    await outbox.MarkPublishedAsync(msg.Id, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to relay outbox message {Id}.", msg.Id);
                    await outbox.MarkFailedAsync(msg.Id, ex.Message, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```
