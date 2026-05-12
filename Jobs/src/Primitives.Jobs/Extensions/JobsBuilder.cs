using Microsoft.Extensions.DependencyInjection;

namespace Primitives.Jobs.Extensions;

/// <summary>Fluent builder returned by <see cref="ServiceCollectionExtensions.AddPrimitivesJobs"/>.</summary>
public sealed class JobsBuilder
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }

    internal JobsBuilder(IServiceCollection services)
        => Services = services;
}
