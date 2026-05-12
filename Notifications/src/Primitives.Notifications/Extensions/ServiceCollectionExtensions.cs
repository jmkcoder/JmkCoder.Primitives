using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Notifications.Abstractions;
using Primitives.Notifications.Internal;

namespace Primitives.Notifications.Extensions;

/// <summary>Extension methods for registering the notifications module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="INotificationService"/>, <see cref="IWebhookDispatcher"/>,
    /// and <see cref="IWebhookEndpointStore"/> (in-memory).
    /// </summary>
    /// <remarks>
    /// Add channels and replace the webhook store:
    /// <code>
    /// services.AddPrimitivesNotifications()
    ///     .AddChannel&lt;SmtpEmailChannel&gt;()
    ///     .AddWebhookEndpointStore&lt;MyDatabaseWebhookStore&gt;();
    /// </code>
    /// </remarks>
    public static NotificationsBuilder AddPrimitivesNotifications(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddHttpClient<WebhookDispatcher>();
        services.TryAddSingleton<IWebhookEndpointStore, InMemoryWebhookEndpointStore>();
        services.TryAddSingleton<IWebhookDispatcher, WebhookDispatcher>();
        services.TryAddSingleton<INotificationService, NotificationService>();
        return new NotificationsBuilder(services);
    }

    /// <summary>Registers a notification channel implementation.</summary>
    public static NotificationsBuilder AddChannel<TChannel>(this NotificationsBuilder builder)
        where TChannel : class, INotificationChannel
    {
        builder.Services.AddSingleton<INotificationChannel, TChannel>();
        return builder;
    }

    /// <summary>Replaces the default <see cref="IWebhookEndpointStore"/> with a custom implementation.</summary>
    public static NotificationsBuilder AddWebhookEndpointStore<TStore>(this NotificationsBuilder builder)
        where TStore : class, IWebhookEndpointStore
    {
        builder.Services.RemoveAll<IWebhookEndpointStore>();
        builder.Services.AddSingleton<IWebhookEndpointStore, TStore>();
        return builder;
    }
}
