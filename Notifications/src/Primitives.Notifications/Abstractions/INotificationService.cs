using Primitives.Notifications.Models;

namespace Primitives.Notifications.Abstractions;

/// <summary>
/// Sends transactional notifications through one or more registered channels.
/// </summary>
public interface INotificationService
{
    /// <summary>Sends <paramref name="notification"/> via all applicable registered channels.</summary>
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
