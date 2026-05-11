using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Messaging.Abstractions;
using Primitives.Messaging.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace Primitives.Messaging.RabbitMQ;

/// <summary>
/// <see cref="IMessagePublisher"/> backed by RabbitMQ.
/// Uses publisher confirms for at-least-once delivery guarantees.
/// </summary>
internal sealed class RabbitMQMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly MessagingOptions _messagingOptions;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly IModel _channel;
    private readonly object _channelLock = new();
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RabbitMQMessagePublisher(
        IConnection connection,
        IOptions<MessagingOptions> messagingOptions,
        ILogger<RabbitMQMessagePublisher> logger)
    {
        _messagingOptions = messagingOptions.Value;
        _logger           = logger;
        _channel          = connection.CreateModel();
        _channel.ConfirmSelect();
    }

    public Task PublishAsync<T>(
        T message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var exchange   = options?.Exchange   ?? _messagingOptions.DefaultExchange;
        var routingKey = options?.RoutingKey ?? typeof(T).Name.ToLowerInvariant();
        var body       = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        var props = _channel.CreateBasicProperties();
        props.MessageId     = Guid.NewGuid().ToString();
        props.CorrelationId = options?.CorrelationId;
        props.Timestamp     = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        props.ContentType   = "application/json";
        props.DeliveryMode  = 2; // persistent
        props.Type          = typeof(T).FullName;

        if (options?.Ttl is { } ttl)
            props.Expiration = ((long)ttl.TotalMilliseconds).ToString();

        if (options?.Headers is { Count: > 0 } headers)
        {
            props.Headers = new Dictionary<string, object>(headers.Count);
            foreach (var (k, v) in headers)
                props.Headers[k] = v;
        }

        lock (_channelLock)
        {
            _channel.BasicPublish(exchange, routingKey, mandatory: false, props, body);
            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(10));
        }

        _logger.LogDebug(
            "Published {Type} to {Exchange}/{RoutingKey} ({MessageId}).",
            typeof(T).Name, exchange, routingKey, props.MessageId);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.Close();
        _channel.Dispose();
    }
}
