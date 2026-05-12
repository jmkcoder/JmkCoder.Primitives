namespace Primitives.Jobs.Models;

/// <summary>Execution status of a job entry.</summary>
public enum JobStatus
{
    /// <summary>Waiting to be picked up by a worker.</summary>
    Pending,

    /// <summary>Currently executing.</summary>
    Running,

    /// <summary>Completed successfully.</summary>
    Succeeded,

    /// <summary>Failed permanently after exhausting retries.</summary>
    Failed,
}
