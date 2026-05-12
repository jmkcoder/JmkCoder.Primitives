namespace Primitives.Jobs.Abstractions;

/// <summary>
/// Enqueues and schedules background jobs.
/// </summary>
public interface IJobScheduler
{
    /// <summary>Enqueues a fire-and-forget job that runs as soon as a worker is available.</summary>
    Task<string> EnqueueAsync<TJob>(CancellationToken cancellationToken = default)
        where TJob : IJob;

    /// <summary>Enqueues a fire-and-forget job with the supplied arguments.</summary>
    Task<string> EnqueueAsync<TJob, TArgs>(TArgs args, CancellationToken cancellationToken = default)
        where TJob : IJob<TArgs>
        where TArgs : notnull;

    /// <summary>Schedules a job to run after the specified delay.</summary>
    Task<string> ScheduleAsync<TJob>(TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IJob;

    /// <summary>Schedules a job with arguments to run after the specified delay.</summary>
    Task<string> ScheduleAsync<TJob, TArgs>(TArgs args, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IJob<TArgs>
        where TArgs : notnull;

    /// <summary>
    /// Registers a recurring job that runs on the given cron schedule.
    /// Calling this method again with the same <paramref name="jobId"/> updates the schedule.
    /// </summary>
    Task AddOrUpdateRecurringAsync<TJob>(string jobId, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IJob;

    /// <summary>Removes a previously registered recurring job.</summary>
    Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default);
}
