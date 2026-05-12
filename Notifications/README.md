# Primitives.Notifications

Transactional notifications and outbound webhooks for SaaS. Channel-agnostic with pluggable transports and HMAC-signed webhook delivery.

## Quick Start

```csharp
builder.Services
    .AddPrimitivesNotifications()
    .AddChannel<SmtpEmailChannel>();
```

## Sending Notifications

```csharp
await notifications.SendAsync(new Notification
{
    Recipient = "user@example.com",
    Subject   = "Welcome!",
    Body      = "Thanks for signing up.",
    Channels  = ["email"],
    TenantId  = tenantId,
});
```

## Custom Channel

```csharp
public sealed class SmtpEmailChannel : INotificationChannel
{
    public string ChannelName => "email";
    public bool CanHandle(Notification n) => n.Recipient.Contains('@');
    public async Task SendAsync(Notification n, CancellationToken ct) { /* ... */ }
}
```

## Webhooks

```csharp
// Register an endpoint
await webhookStore.UpsertAsync(new WebhookEndpoint
{
    TenantId   = tenantId,
    Url        = "https://customer.example.com/hooks",
    EventTypes = ["invoice.paid"],
    Secret     = "super-secret-key",
});

// Dispatch an event — delivers with X-Webhook-Signature (HMAC-SHA256)
await webhookDispatcher.DispatchAsync(new WebhookEvent
{
    EventType = "invoice.paid",
    TenantId  = tenantId,
    Payload   = new { InvoiceId = "inv-123", Amount = 99.99 },
});
```
