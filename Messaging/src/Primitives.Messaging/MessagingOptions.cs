namespace Primitives.Messaging;

/// <summary>Global defaults for the Primitives.Messaging library.</summary>
public sealed class MessagingOptions
{
    /// <summary>
    /// Default exchange to publish to when <see cref="Models.PublishOptions.Exchange"/> is not
    /// specified. Set to <c>string.Empty</c> to use the broker's default (nameless) exchange.
    /// </summary>
    public string DefaultExchange { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of delivery attempts before a message is considered permanently failed.
    /// Broker-specific providers use this when configuring retry or dead-letter behaviour.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Delay between retry attempts.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Name of the dead-letter exchange. Messages that exceed <see cref="MaxRetryAttempts"/>
    /// are forwarded here. Only used by broker-specific providers.
    /// </summary>
    public string DeadLetterExchange { get; set; } = "dead-letter";
}
