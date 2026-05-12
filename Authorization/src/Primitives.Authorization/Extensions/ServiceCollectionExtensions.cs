using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Authorization.Abstractions;
using Primitives.Authorization.Internal;

namespace Primitives.Authorization.Extensions;

/// <summary>Extension methods for registering the authorization module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IPermissionService"/>, <see cref="IRoleStore"/> (in-memory),
    /// and <see cref="IPermissionStore"/> (in-memory).
    /// </summary>
    /// <remarks>
    /// Seed roles inline via <paramref name="configure"/>, or replace the stores:
    /// <code>
    /// services.AddPrimitivesAuthorization(opts =>
    ///     opts.Roles.Add(new Role { Name = "admin", TenantId = "t1", Permissions = ["invoices:read"] }))
    ///     .AddRoleStore&lt;MyRoleStore&gt;();
    /// </code>
    /// </remarks>
    public static AuthorizationBuilder AddPrimitivesAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<AuthorizationOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IRoleStore, InMemoryRoleStore>();
        services.TryAddSingleton<IPermissionStore, InMemoryPermissionStore>();
        services.TryAddSingleton<IPermissionService, PermissionService>();
        services.AddHostedService<RoleSeedService>();
        return new AuthorizationBuilder(services);
    }

    /// <summary>Replaces the default <see cref="IRoleStore"/> with a custom implementation.</summary>
    public static AuthorizationBuilder AddRoleStore<TStore>(this AuthorizationBuilder builder)
        where TStore : class, IRoleStore
    {
        builder.Services.RemoveAll<IRoleStore>();
        builder.Services.AddSingleton<IRoleStore, TStore>();
        return builder;
    }

    /// <summary>Replaces the default <see cref="IPermissionStore"/> with a custom implementation.</summary>
    public static AuthorizationBuilder AddPermissionStore<TStore>(this AuthorizationBuilder builder)
        where TStore : class, IPermissionStore
    {
        builder.Services.RemoveAll<IPermissionStore>();
        builder.Services.AddSingleton<IPermissionStore, TStore>();
        return builder;
    }
}
