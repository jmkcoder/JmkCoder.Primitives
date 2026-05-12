using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Auditing.Abstractions;
using Primitives.Auditing.Internal;

namespace Primitives.Auditing.Extensions;

/// <summary>Extension methods for registering the auditing module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAuditLogger"/> and <see cref="IAuditStore"/> (in-memory by default).
    /// </summary>
    /// <remarks>
    /// Replace the store for production use:
    /// <code>
    /// services.AddPrimitivesAuditing().AddAuditStore&lt;MyDatabaseAuditStore&gt;();
    /// </code>
    /// </remarks>
    public static AuditingBuilder AddPrimitivesAuditing(this IServiceCollection services)
    {
        services.AddLogging();
        services.TryAddSingleton<IAuditStore, InMemoryAuditStore>();
        services.TryAddSingleton<IAuditLogger, AuditLogger>();
        return new AuditingBuilder(services);
    }

    /// <summary>Replaces the default <see cref="IAuditStore"/> with a custom implementation.</summary>
    public static AuditingBuilder AddAuditStore<TStore>(this AuditingBuilder builder)
        where TStore : class, IAuditStore
    {
        builder.Services.RemoveAll<IAuditStore>();
        builder.Services.AddSingleton<IAuditStore, TStore>();
        return builder;
    }
}
