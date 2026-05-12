using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Primitives.Notifications.Abstractions;
using Primitives.Notifications.Models;

namespace Primitives.Notifications.Internal;

/// <summary>
/// Dispatches webhook events to tenant-registered HTTP endpoints with HMAC-SHA256 signatures.
/// </summary>
internal sealed class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IWebhookEndpointStore _store;
    private readonly HttpClient _http;
    private readonly ILogger<WebhookDispatcher> _logger;

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public WebhookDispatcher(
        IWebhookEndpointStore store,
        HttpClient http,
        ILogger<WebhookDispatcher> logger)
    {
        _store  = store;
        _http   = http;
        _logger = logger;
    }

    public async Task DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var endpoints = await _store.GetEndpointsAsync(
            webhookEvent.TenantId, webhookEvent.EventType, cancellationToken).ConfigureAwait(false);

        foreach (var endpoint in endpoints)
            await DeliverAsync(webhookEvent, endpoint, cancellationToken).ConfigureAwait(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task DeliverAsync(WebhookEvent webhookEvent, WebhookEndpoint endpoint, CancellationToken cancellationToken)
    {
        var payload   = JsonSerializer.Serialize(webhookEvent, _jsonOptions);
        var signature = ComputeSignature(payload, endpoint.Secret);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Webhook-Id",        webhookEvent.Id.ToString());
        request.Headers.Add("X-Webhook-Event",     webhookEvent.EventType);
        request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
        request.Headers.Add("X-Webhook-Timestamp", webhookEvent.OccurredAt.ToUnixTimeSeconds().ToString());

        try
        {
            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                _logger.LogDebug("Webhook delivered: {EventType} → {Url}", webhookEvent.EventType, endpoint.Url);
            else
                _logger.LogWarning("Webhook delivery failed ({Status}): {EventType} → {Url}",
                    (int)response.StatusCode, webhookEvent.EventType, endpoint.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery threw: {EventType} → {Url}", webhookEvent.EventType, endpoint.Url);
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
