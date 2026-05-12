using Microsoft.Extensions.Logging;
using Primitives.Notifications.Abstractions;
using Primitives.Notifications.Models;

namespace Primitives.Notifications.Internal;

/// <summary>Default <see cref="INotificationService"/> that fans out to all applicable channels.</summary>
internal sealed class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationService> logger)
    {
        _channels = channels;
        _logger   = logger;
    }

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var applicable = _channels
            .Where(c => notification.Channels.Count == 0
                        || notification.Channels.Contains(c.ChannelName, StringComparer.OrdinalIgnoreCase))
            .Where(c => c.CanHandle(notification))
            .ToList();

        if (applicable.Count == 0)
        {
            _logger.LogWarning("No channel could handle notification for recipient '{Recipient}'", notification.Recipient);
            return;
        }

        foreach (var channel in applicable)
        {
            try
            {
                await channel.SendAsync(notification, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Notification sent via '{Channel}' to '{Recipient}'", channel.ChannelName, notification.Recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Channel '{Channel}' failed to deliver notification to '{Recipient}'",
                    channel.ChannelName, notification.Recipient);
            }
        }
    }
}
