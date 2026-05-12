using Microsoft.Extensions.DependencyInjection;

namespace Primitives.Authorization.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesAuthorization"/>.</summary>
public sealed class AuthorizationBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal AuthorizationBuilder(IServiceCollection services)
        => Services = services;
}
