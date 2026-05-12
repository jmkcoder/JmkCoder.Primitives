using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Jobs.Abstractions;
using Primitives.Jobs.Models;

namespace Primitives.Jobs.Internal;

/// <summary>Default <see cref="IJobScheduler"/> backed by <see cref="IJobStore"/>.</summary>
internal sealed class JobScheduler : IJobScheduler
{
    private readonly IJobStore _store;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(IJobStore store, ILogger<JobScheduler> logger)
    {
        _store  = store;
        _logger = logger;
    }

    public Task<string> EnqueueAsync<TJob>(CancellationToken cancellationToken = default)
        where TJob : IJob
        => ScheduleInternalAsync(typeof(TJob).AssemblyQualifiedName!, argsJson: null, runAt: DateTimeOffset.UtcNow, cron: null, cancellationToken);

    public Task<string> EnqueueAsync<TJob, TArgs>(TArgs args, CancellationToken cancellationToken = default)
        where TJob : IJob<TArgs>
        where TArgs : notnull
        => ScheduleInternalAsync(typeof(TJob).AssemblyQualifiedName!, JsonSerializer.Serialize(args), DateTimeOffset.UtcNow, cron: null, cancellationToken);

    public Task<string> ScheduleAsync<TJob>(TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IJob
        => ScheduleInternalAsync(typeof(TJob).AssemblyQualifiedName!, argsJson: null, DateTimeOffset.UtcNow.Add(delay), cron: null, cancellationToken);

    public Task<string> ScheduleAsync<TJob, TArgs>(TArgs args, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : IJob<TArgs>
        where TArgs : notnull
        => ScheduleInternalAsync(typeof(TJob).AssemblyQualifiedName!, JsonSerializer.Serialize(args), DateTimeOffset.UtcNow.Add(delay), cron: null, cancellationToken);

    public async Task AddOrUpdateRecurringAsync<TJob>(string jobId, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IJob
        => await ScheduleInternalAsync(typeof(TJob).AssemblyQualifiedName!, argsJson: null, DateTimeOffset.UtcNow, cronExpression, cancellationToken, jobId).ConfigureAwait(false);

    public Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default)
        => _store.RemoveRecurringAsync(jobId, cancellationToken);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> ScheduleInternalAsync(
        string jobType,
        string? argsJson,
        DateTimeOffset runAt,
        string? cron,
        CancellationToken cancellationToken,
        string? id = null)
    {
        var entry = new JobEntry
        {
            Id             = id ?? Guid.NewGuid().ToString(),
            JobType        = jobType,
            ArgsJson       = argsJson,
            RunAt          = runAt,
            CronExpression = cron,
        };

        await _store.SaveAsync(entry, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Job enqueued: {JobId} ({JobType}) at {RunAt}", entry.Id, jobType, runAt);
        return entry.Id;
    }
}
