using Primitives.Notifications.Models;

namespace Primitives.Notifications.Abstractions;

/// <summary>
/// Stores webhook endpoint registrations for tenants.
/// Replace the default in-memory store with a database-backed implementation for production.
/// </summary>
public interface IWebhookEndpointStore
{
    /// <summary>Returns all endpoints registered for <paramref name="tenantId"/> and <paramref name="eventType"/>.</summary>
    Task<IReadOnlyList<WebhookEndpoint>> GetEndpointsAsync(string tenantId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>Registers or replaces a webhook endpoint.</summary>
    Task UpsertAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken = default);

    /// <summary>Removes an endpoint by its identifier.</summary>
    Task DeleteAsync(string endpointId, CancellationToken cancellationToken = default);
}
