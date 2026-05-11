using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Messaging.Abstractions;
using Primitives.Messaging.Internal;
using Primitives.Messaging.Models;
using System.Text.Json;

namespace Primitives.Messaging.Extensions;

/// <summary>Extension methods for registering Primitives.Messaging with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Registers Primitives.Messaging with the in-process <see cref="IMessagePublisher"/>.
    /// Suitable for development, testing, and single-process scenarios.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="MessagingOptions"/>.</param>
    public static IServiceCollection AddPrimitivesMessaging(
        this IServiceCollection services,
        Action<MessagingOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<MessagingOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
        return services;
    }

    /// <summary>
    /// Registers an <see cref="IMessageHandler{T}"/> and its queue/exchange binding.
    /// The handler is invoked by the in-process publisher and by broker-specific consumer
    /// services (e.g. <c>RabbitMQConsumerService</c>).
    /// </summary>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <typeparam name="TMessage">The message payload type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="queueName">
    /// Queue (or subscription topic) to consume from.
    /// Defaults to the message type name in lowercase when not specified.
    /// </param>
    /// <param name="exchange">
    /// Exchange to bind to. Defaults to <see cref="MessagingOptions.DefaultExchange"/> when
    /// not specified.
    /// </param>
    /// <param name="routingKey">
    /// Routing key for the queue binding. Defaults to the message type name in lowercase.
    /// </param>
    /// <param name="options">Subscription-level options (prefetch count, etc.).</param>
    public static IServiceCollection AddMessageHandler<THandler, TMessage>(
        this IServiceCollection services,
        string? queueName   = null,
        string? exchange    = null,
        string? routingKey  = null,
        SubscriptionOptions? options = null)
        where THandler : class, IMessageHandler<TMessage>
        where TMessage : notnull
    {
        services.AddTransient<IMessageHandler<TMessage>, THandler>();

        var effectiveQueue   = queueName  ?? typeof(TMessage).Name.ToLowerInvariant();
        var effectiveRouting = routingKey ?? typeof(TMessage).Name.ToLowerInvariant();
        var effectiveOpts    = options    ?? new SubscriptionOptions();

        services.AddSingleton(new MessageHandlerRegistration
        {
            QueueName  = effectiveQueue,
            Exchange   = exchange ?? string.Empty,
            RoutingKey = effectiveRouting,
            Options    = effectiveOpts,
            Dispatch   = async (sp, body, metadata, ct) =>
            {
                var message = JsonSerializer.Deserialize<TMessage>(body.Span, JsonOptions)
                    ?? throw new InvalidOperationException(
                        $"Failed to deserialize {typeof(TMessage).Name} from message body.");

                var context = new MessageContext<TMessage>
                {
                    Message  = message,
                    Metadata = metadata,
                };

                using var scope = sp.CreateScope();
                var handlers    = scope.ServiceProvider.GetServices<IMessageHandler<TMessage>>();
                var result      = ConsumeResult.Ack;

                foreach (var handler in handlers)
                {
                    result = await handler.HandleAsync(context, ct).ConfigureAwait(false);
                    if (result != ConsumeResult.Ack)
                        break;
                }

                return result;
            },
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="InMemoryOutboxStore"/> as <see cref="IOutboxStore"/>.
    /// Suitable for development and testing only — state is lost on restart.
    /// </summary>
    public static IServiceCollection AddInMemoryOutbox(this IServiceCollection services)
    {
        services.TryAddSingleton<IOutboxStore, InMemoryOutboxStore>();
        return services;
    }
}
