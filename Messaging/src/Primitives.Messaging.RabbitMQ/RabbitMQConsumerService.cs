using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Messaging.Internal;
using Primitives.Messaging.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Primitives.Messaging.RabbitMQ;

/// <summary>
/// Background service that starts RabbitMQ consumers for every registered
/// <see cref="MessageHandlerRegistration"/>.
/// </summary>
internal sealed class RabbitMQConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IEnumerable<MessageHandlerRegistration> _registrations;
    private readonly IServiceProvider _services;
    private readonly MessagingOptions _messagingOptions;
    private readonly RabbitMQOptions _rabbitOptions;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly List<IModel> _channels = [];

    public RabbitMQConsumerService(
        IConnection connection,
        IEnumerable<MessageHandlerRegistration> registrations,
        IServiceProvider services,
        IOptions<MessagingOptions> messagingOptions,
        IOptions<RabbitMQOptions> rabbitOptions,
        ILogger<RabbitMQConsumerService> logger)
    {
        _connection       = connection;
        _registrations    = registrations;
        _services         = services;
        _messagingOptions = messagingOptions.Value;
        _rabbitOptions    = rabbitOptions.Value;
        _logger           = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var reg in _registrations)
            StartConsumer(reg, stoppingToken);

        return Task.CompletedTask;
    }

    private void StartConsumer(MessageHandlerRegistration reg, CancellationToken stoppingToken)
    {
        var channel = _connection.CreateModel();
        _channels.Add(channel);

        channel.BasicQos(prefetchSize: 0, reg.Options.PrefetchCount, global: false);

        if (_rabbitOptions.DeclareTopology)
        {
            if (!string.IsNullOrEmpty(reg.Exchange))
            {
                channel.ExchangeDeclare(
                    exchange:   reg.Exchange,
                    type:       _rabbitOptions.ExchangeType,
                    durable:    true,
                    autoDelete: false);
            }

            channel.QueueDeclare(
                queue:      reg.QueueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = _messagingOptions.DeadLetterExchange,
                });

            if (!string.IsNullOrEmpty(reg.Exchange))
                channel.QueueBind(reg.QueueName, reg.Exchange, reg.RoutingKey);
        }

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (_, ea) =>
        {
            _ = DispatchAsync(channel, reg, ea, stoppingToken);
        };

        channel.BasicConsume(reg.QueueName, autoAck: false, consumer);

        _logger.LogInformation(
            "Started consumer on queue {Queue} (exchange: {Exchange}, routing: {RoutingKey}).",
            reg.QueueName, reg.Exchange, reg.RoutingKey);
    }

    private async Task DispatchAsync(
        IModel channel,
        MessageHandlerRegistration reg,
        BasicDeliverEventArgs ea,
        CancellationToken ct)
    {
        var metadata = new MessageMetadata
        {
            MessageId     = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString(),
            CorrelationId = ea.BasicProperties.CorrelationId,
            Timestamp     = ea.BasicProperties.IsTimestampPresent()
                                ? DateTimeOffset.FromUnixTimeSeconds(ea.BasicProperties.Timestamp.UnixTime)
                                : DateTimeOffset.UtcNow,
            DeliveryCount = ea.Redelivered ? 2 : 1,
        };

        ConsumeResult result;
        try
        {
            result = await reg.Dispatch(_services, ea.Body, metadata, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception dispatching message from queue {Queue}. Nacking.",
                reg.QueueName);
            result = ConsumeResult.Nack;
        }

        switch (result)
        {
            case ConsumeResult.Ack:
                channel.BasicAck(ea.DeliveryTag, multiple: false);
                break;
            case ConsumeResult.Requeue:
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                break;
            default: // ConsumeResult.Nack
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                break;
        }
    }

    public override void Dispose()
    {
        foreach (var ch in _channels)
        {
            try { ch.Close(); }
            catch (Exception ex) { _logger.LogDebug(ex, "Error closing channel during dispose."); }
            ch.Dispose();
        }
        base.Dispose();
    }
}
