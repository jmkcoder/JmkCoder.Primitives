using System.Collections.Concurrent;
using Primitives.Notifications.Abstractions;
using Primitives.Notifications.Models;

namespace Primitives.Notifications.Internal;

/// <summary>Thread-safe in-memory webhook endpoint store.</summary>
internal sealed class InMemoryWebhookEndpointStore : IWebhookEndpointStore
{
    private readonly ConcurrentDictionary<string, WebhookEndpoint> _endpoints =
        new(StringComparer.Ordinal);

    public Task<IReadOnlyList<WebhookEndpoint>> GetEndpointsAsync(
        string tenantId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var matches = _endpoints.Values
            .Where(e => e.IsActive
                     && string.Equals(e.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)
                     && (e.EventTypes.Contains("*") || e.EventTypes.Contains(eventType, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult<IReadOnlyList<WebhookEndpoint>>(matches);
    }

    public Task UpsertAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        _endpoints[endpoint.Id] = endpoint;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string endpointId, CancellationToken cancellationToken = default)
    {
        _endpoints.TryRemove(endpointId, out _);
        return Task.CompletedTask;
    }
}
