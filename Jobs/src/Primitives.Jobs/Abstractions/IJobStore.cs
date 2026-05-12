using Primitives.Jobs.Models;

namespace Primitives.Jobs.Abstractions;

/// <summary>
/// Persistent queue used by the job engine to store and retrieve pending job entries.
/// Replace the default in-memory store for durable, distributed deployments.
/// </summary>
public interface IJobStore
{
    /// <summary>Persists a new job entry.</summary>
    Task SaveAsync(JobEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Returns pending jobs that are due to run, up to <paramref name="batchSize"/>.</summary>
    Task<IReadOnlyList<JobEntry>> GetDueAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marks a job as completed successfully.</summary>
    Task MarkSucceededAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Marks a job as failed with the given error message.</summary>
    Task MarkFailedAsync(string jobId, string error, CancellationToken cancellationToken = default);

    /// <summary>Reschedules a recurring job's next run time.</summary>
    Task RescheduleRecurringAsync(string jobId, DateTimeOffset nextRunAt, CancellationToken cancellationToken = default);

    /// <summary>Removes a recurring job definition.</summary>
    Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default);
}
