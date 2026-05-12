using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Billing.Abstractions;
using Primitives.Billing.Internal;

namespace Primitives.Billing.Extensions;

/// <summary>Extension methods for registering the billing and metering module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IUsageMeter"/>, <see cref="IEntitlementService"/>,
    /// <see cref="IUsageStore"/> (in-memory), and <see cref="IEntitlementStore"/> (in-memory).
    /// </summary>
    /// <remarks>
    /// Replace the stores for production:
    /// <code>
    /// services.AddPrimitivesBilling()
    ///     .AddEntitlementStore&lt;MyDatabaseEntitlementStore&gt;()
    ///     .AddUsageStore&lt;MyDatabaseUsageStore&gt;();
    /// </code>
    /// </remarks>
    public static BillingBuilder AddPrimitivesBilling(this IServiceCollection services)
    {
        services.AddLogging();
        services.TryAddSingleton<IUsageStore, InMemoryUsageStore>();
        services.TryAddSingleton<IEntitlementStore, InMemoryEntitlementStore>();
        services.TryAddSingleton<IUsageMeter, UsageMeter>();
        services.TryAddSingleton<IEntitlementService, EntitlementService>();
        return new BillingBuilder(services);
    }

    /// <summary>Replaces the default <see cref="IEntitlementStore"/> with a custom implementation.</summary>
    public static BillingBuilder AddEntitlementStore<TStore>(this BillingBuilder builder)
        where TStore : class, IEntitlementStore
    {
        builder.Services.RemoveAll<IEntitlementStore>();
        builder.Services.AddSingleton<IEntitlementStore, TStore>();
        return builder;
    }

    /// <summary>Replaces the default <see cref="IUsageStore"/> with a custom implementation.</summary>
    public static BillingBuilder AddUsageStore<TStore>(this BillingBuilder builder)
        where TStore : class, IUsageStore
    {
        builder.Services.RemoveAll<IUsageStore>();
        builder.Services.AddSingleton<IUsageStore, TStore>();
        return builder;
    }
}
