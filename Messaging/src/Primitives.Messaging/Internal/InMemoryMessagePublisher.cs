using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Messaging.Abstractions;
using Primitives.Messaging.Models;

namespace Primitives.Messaging.Internal;

/// <summary>
/// In-process <see cref="IMessagePublisher"/> that dispatches directly to registered
/// <see cref="IMessageHandler{T}"/> instances. Suitable for development, testing, and
/// single-process scenarios where a broker is not required.
/// </summary>
internal sealed class InMemoryMessagePublisher : IMessagePublisher
{
    private readonly IServiceProvider _services;
    private readonly MessagingOptions _options;
    private readonly ILogger<InMemoryMessagePublisher> _logger;

    public InMemoryMessagePublisher(
        IServiceProvider services,
        IOptions<MessagingOptions> options,
        ILogger<InMemoryMessagePublisher> logger)
    {
        _services = services;
        _options  = options.Value;
        _logger   = logger;
    }

    public async Task PublishAsync<T>(
        T message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        var metadata = new MessageMetadata
        {
            MessageId     = Guid.NewGuid().ToString(),
            CorrelationId = options?.CorrelationId,
            Timestamp     = DateTimeOffset.UtcNow,
            DeliveryCount = 1,
        };

        var context = new MessageContext<T> { Message = message, Metadata = metadata };

        using var scope  = _services.CreateScope();
        var handlers     = scope.ServiceProvider.GetServices<IMessageHandler<T>>().ToList();

        if (handlers.Count == 0)
        {
            _logger.LogDebug(
                "No handlers registered for {MessageType}; message dropped.",
                typeof(T).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            var result = await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Handler {Handler} processed {MessageType} ({MessageId}): {Result}.",
                handler.GetType().Name, typeof(T).Name, metadata.MessageId, result);

            if (result == ConsumeResult.Nack)
                _logger.LogWarning(
                    "Handler {Handler} nacked {MessageType} ({MessageId}).",
                    handler.GetType().Name, typeof(T).Name, metadata.MessageId);
        }
    }
}
