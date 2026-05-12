using Primitives.Notifications.Models;

namespace Primitives.Notifications.Abstractions;

/// <summary>
/// Delivers a notification through a specific transport (email, SMS, push, etc.).
/// Register one or more implementations; all applicable channels are invoked per notification.
/// </summary>
public interface INotificationChannel
{
    /// <summary>Channel identifier (e.g. <c>"email"</c>, <c>"sms"</c>).</summary>
    string ChannelName { get; }

    /// <summary>Returns <see langword="true"/> when this channel can deliver <paramref name="notification"/>.</summary>
    bool CanHandle(Notification notification);

    /// <summary>Delivers the notification.</summary>
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
