namespace Primitives.Jobs.Models;

/// <summary>A persisted job entry held by <see cref="Abstractions.IJobStore"/>.</summary>
public sealed record JobEntry
{
    /// <summary>Unique job identifier.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Assembly-qualified type name of the job class.</summary>
    public required string JobType { get; init; }

    /// <summary>JSON-serialized arguments, or <see langword="null"/> for argument-less jobs.</summary>
    public string? ArgsJson { get; init; }

    /// <summary>UTC time after which the job may be executed.</summary>
    public DateTimeOffset RunAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Current execution status.</summary>
    public JobStatus Status { get; init; } = JobStatus.Pending;

    /// <summary>Number of execution attempts.</summary>
    public int AttemptCount { get; init; }

    /// <summary>Last error message, if the job failed.</summary>
    public string? LastError { get; init; }

    /// <summary>Cron expression for recurring jobs; <see langword="null"/> for one-off jobs.</summary>
    public string? CronExpression { get; init; }

    /// <summary>UTC time the entry was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
