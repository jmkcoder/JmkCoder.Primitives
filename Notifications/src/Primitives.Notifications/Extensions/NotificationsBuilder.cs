using Microsoft.Extensions.DependencyInjection;

namespace Primitives.Notifications.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesNotifications"/>.</summary>
public sealed class NotificationsBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal NotificationsBuilder(IServiceCollection services)
        => Services = services;
}
