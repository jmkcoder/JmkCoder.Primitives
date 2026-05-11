using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Primitives.Messaging.Abstractions;
using RabbitMQ.Client;

namespace Primitives.Messaging.RabbitMQ.Extensions;

/// <summary>
/// Extension methods for registering the RabbitMQ messaging provider with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Primitives.Messaging with a RabbitMQ publisher and consumer background service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureRabbit">Delegate to configure <see cref="RabbitMQOptions"/>.</param>
    /// <param name="configureMessaging">
    /// Optional delegate to configure global <see cref="MessagingOptions"/>.
    /// </param>
    public static IServiceCollection AddPrimitivesMessagingRabbitMQ(
        this IServiceCollection services,
        Action<RabbitMQOptions> configureRabbit,
        Action<MessagingOptions>? configureMessaging = null)
    {
        services.AddLogging();
        services.Configure<MessagingOptions>(configureMessaging ?? (_ => { }));
        services.Configure<RabbitMQOptions>(configureRabbit);

        services.TryAddSingleton<IConnection>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

            if (opts.ConnectionFactoryFactory is not null)
                return opts.ConnectionFactoryFactory(sp).CreateConnection();

            return new ConnectionFactory
            {
                HostName           = opts.HostName,
                Port               = opts.Port,
                UserName           = opts.UserName,
                Password           = opts.Password,
                VirtualHost        = opts.VirtualHost,
                ClientProvidedName = opts.ClientProvidedName,
            }.CreateConnection();
        });

        services.TryAddSingleton<IMessagePublisher, RabbitMQMessagePublisher>();
        services.AddHostedService<RabbitMQConsumerService>();

        return services;
    }
}
