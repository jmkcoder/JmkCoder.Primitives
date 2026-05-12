namespace Primitives.Jobs;

/// <summary>Top-level configuration for the jobs module.</summary>
public sealed class JobsOptions
{
    /// <summary>How often the worker polls for due jobs. Defaults to 10 seconds.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Maximum number of jobs processed per polling cycle. Defaults to 10.</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Maximum number of retry attempts before a job is permanently failed. Defaults to 3.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Delay before retrying a failed job. Defaults to 30 seconds.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
