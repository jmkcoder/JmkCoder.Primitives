using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Jobs.Abstractions;
using Primitives.Jobs.Internal;

namespace Primitives.Jobs.Extensions;

/// <summary>Extension methods for registering the background jobs module.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IJobScheduler"/>, <see cref="IJobStore"/> (in-memory),
    /// and the background <see cref="JobWorker"/> hosted service.
    /// </summary>
    /// <remarks>
    /// Register job implementations in DI so the worker can resolve them:
    /// <code>
    /// services.AddPrimitivesJobs()
    ///     .AddJob&lt;SendWelcomeEmailJob&gt;()
    ///     .AddJobStore&lt;MyDatabaseJobStore&gt;();
    /// </code>
    /// </remarks>
    public static JobsBuilder AddPrimitivesJobs(
        this IServiceCollection services,
        Action<JobsOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<JobsOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IJobStore, InMemoryJobStore>();
        services.TryAddSingleton<IJobScheduler, JobScheduler>();
        services.AddHostedService<JobWorker>();
        return new JobsBuilder(services);
    }

    /// <summary>Registers a job implementation so the worker can resolve it by type.</summary>
    public static JobsBuilder AddJob<TJob>(this JobsBuilder builder)
        where TJob : class
    {
        builder.Services.TryAddTransient<TJob>();
        return builder;
    }

    /// <summary>Replaces the default <see cref="IJobStore"/> with a custom implementation.</summary>
    public static JobsBuilder AddJobStore<TStore>(this JobsBuilder builder)
        where TStore : class, IJobStore
    {
        builder.Services.RemoveAll<IJobStore>();
        builder.Services.AddSingleton<IJobStore, TStore>();
        return builder;
    }
}
