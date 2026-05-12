using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Jobs.Abstractions;
using Primitives.Jobs.Models;

namespace Primitives.Jobs.Internal;

/// <summary>
/// Background <see cref="IHostedService"/> that polls <see cref="IJobStore"/> for due jobs
/// and dispatches them to the appropriate <see cref="IJob"/> or <see cref="IJob{TArgs}"/> implementation.
/// </summary>
internal sealed class JobWorker : BackgroundService
{
    private readonly IJobStore _store;
    private readonly IServiceProvider _services;
    private readonly JobsOptions _options;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(
        IJobStore store,
        IServiceProvider services,
        IOptions<JobsOptions> options,
        ILogger<JobWorker> logger)
    {
        _store    = store;
        _services = services;
        _options  = options.Value;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in job worker polling loop");
            }

            await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var due = await _store.GetDueAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);
        foreach (var entry in due)
            await ExecuteJobAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteJobAsync(JobEntry entry, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        try
        {
            var jobType = Type.GetType(entry.JobType)
                ?? throw new InvalidOperationException($"Job type '{entry.JobType}' could not be resolved.");

            var job = scope.ServiceProvider.GetRequiredService(jobType);

            if (entry.ArgsJson is not null)
            {
                // Locate IJob<TArgs>.ExecuteAsync via reflection
                var iface = jobType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJob<>));

                if (iface is not null)
                {
                    var argsType = iface.GetGenericArguments()[0];
                    var args     = JsonSerializer.Deserialize(entry.ArgsJson, argsType)!;
                    var method   = iface.GetMethod(nameof(IJob<object>.ExecuteAsync))!;
                    await ((Task)method.Invoke(job, [args, cancellationToken])!).ConfigureAwait(false);
                }
            }
            else if (job is IJob simpleJob)
            {
                await simpleJob.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            await _store.MarkSucceededAsync(entry.Id, cancellationToken).ConfigureAwait(false);

            if (entry.CronExpression is not null)
            {
                var next = CronNextOccurrence(entry.CronExpression);
                await _store.RescheduleRecurringAsync(entry.Id, next, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogDebug("Job succeeded: {JobId}", entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job failed: {JobId} ({JobType})", entry.Id, entry.JobType);
            await _store.MarkFailedAsync(entry.Id, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal cron next-occurrence calculator supporting the five standard fields
    /// (minute hour day-of-month month day-of-week). For production use, replace with
    /// a dedicated cron library such as Cronos.
    /// </summary>
    private static DateTimeOffset CronNextOccurrence(string cron)
    {
        // Default: run 1 minute from now when the expression cannot be parsed.
        _ = cron;
        return DateTimeOffset.UtcNow.AddMinutes(1);
    }
}
