using Microsoft.Extensions.DependencyInjection;

namespace Primitives.Auditing.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesAuditing"/>.</summary>
public sealed class AuditingBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal AuditingBuilder(IServiceCollection services)
        => Services = services;
}
