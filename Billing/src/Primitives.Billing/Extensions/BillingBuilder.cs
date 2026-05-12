using Microsoft.Extensions.DependencyInjection;

namespace Primitives.Billing.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesBilling"/>.</summary>
public sealed class BillingBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal BillingBuilder(IServiceCollection services)
        => Services = services;
}
