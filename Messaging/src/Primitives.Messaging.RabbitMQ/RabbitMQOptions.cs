using RabbitMQ.Client;

namespace Primitives.Messaging.RabbitMQ;

/// <summary>Configuration options for the RabbitMQ provider.</summary>
public sealed class RabbitMQOptions
{
    /// <summary>RabbitMQ server hostname. Default: <c>localhost</c>.</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>AMQP port. Default: <c>5672</c>.</summary>
    public int Port { get; set; } = 5672;

    /// <summary>Broker username. Default: <c>guest</c>.</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>Broker password. Default: <c>guest</c>.</summary>
    public string Password { get; set; } = "guest";

    /// <summary>Virtual host. Default: <c>/</c>.</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Optional client-provided name shown in the RabbitMQ management UI.</summary>
    public string? ClientProvidedName { get; set; }

    /// <summary>
    /// Custom connection factory override. When set, all other connection properties are ignored
    /// and the factory returned by this delegate is used instead.
    /// </summary>
    public Func<IServiceProvider, IConnectionFactory>? ConnectionFactoryFactory { get; set; }

    /// <summary>
    /// When <c>true</c>, exchanges and queue bindings are declared on consumer startup.
    /// Set to <c>false</c> when exchanges and queues are managed externally (e.g. via
    /// infrastructure-as-code or the management plugin).
    /// Default: <c>true</c>.
    /// </summary>
    public bool DeclareTopology { get; set; } = true;

    /// <summary>
    /// Exchange type used when auto-declaring exchanges. Default: <c>topic</c>.
    /// </summary>
    public string ExchangeType { get; set; } = global::RabbitMQ.Client.ExchangeType.Topic;
}
