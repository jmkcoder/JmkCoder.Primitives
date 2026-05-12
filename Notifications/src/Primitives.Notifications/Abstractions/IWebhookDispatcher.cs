using Primitives.Notifications.Models;

namespace Primitives.Notifications.Abstractions;

/// <summary>
/// Dispatches outbound webhook events to per-tenant registered endpoints.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>Dispatches <paramref name="webhookEvent"/> to all endpoints registered for its tenant and event type.</summary>
    Task DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
