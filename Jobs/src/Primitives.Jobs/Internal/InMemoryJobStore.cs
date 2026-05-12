using System.Collections.Concurrent;
using Primitives.Jobs.Abstractions;
using Primitives.Jobs.Models;

namespace Primitives.Jobs.Internal;

/// <summary>Thread-safe in-memory job store. Not suitable for production use.</summary>
internal sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<string, JobEntry> _entries = new(StringComparer.Ordinal);

    public Task SaveAsync(JobEntry entry, CancellationToken cancellationToken = default)
    {
        _entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobEntry>> GetDueAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var due = _entries.Values
            .Where(e => e.Status == JobStatus.Pending && e.RunAt <= now)
            .OrderBy(e => e.RunAt)
            .Take(batchSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<JobEntry>>(due);
    }

    public Task MarkSucceededAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(jobId, out var entry))
            _entries[jobId] = entry with { Status = JobStatus.Succeeded };
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(string jobId, string error, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(jobId, out var entry))
            _entries[jobId] = entry with { Status = JobStatus.Failed, LastError = error };
        return Task.CompletedTask;
    }

    public Task RescheduleRecurringAsync(string jobId, DateTimeOffset nextRunAt, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(jobId, out var entry))
            _entries[jobId] = entry with { Status = JobStatus.Pending, RunAt = nextRunAt, AttemptCount = 0 };
        return Task.CompletedTask;
    }

    public Task RemoveRecurringAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _entries.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }
}
