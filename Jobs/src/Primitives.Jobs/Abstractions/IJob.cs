namespace Primitives.Jobs.Abstractions;

/// <summary>Marker interface for jobs that carry no arguments.</summary>
public interface IJob
{
    /// <summary>Executes the job.</summary>
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>Marker interface for jobs that accept strongly-typed arguments.</summary>
public interface IJob<TArgs> where TArgs : notnull
{
    /// <summary>Executes the job with the provided <paramref name="args"/>.</summary>
    Task ExecuteAsync(TArgs args, CancellationToken cancellationToken);
}
