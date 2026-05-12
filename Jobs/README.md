# Primitives.Jobs

Background job scheduling for SaaS — fire-and-forget, delayed, and recurring (cron) jobs with pluggable execution backends.

## Quick Start

```csharp
builder.Services
    .AddPrimitivesJobs(opts => opts.PollingInterval = TimeSpan.FromSeconds(5))
    .AddJob<SendWelcomeEmailJob>()
    .AddJob<GenerateInvoiceJob>();
```

## Defining a Job

```csharp
public sealed class SendWelcomeEmailJob : IJob<WelcomeEmailArgs>
{
    public async Task ExecuteAsync(WelcomeEmailArgs args, CancellationToken cancellationToken)
    {
        // send email ...
    }
}
```

## Enqueuing Jobs

```csharp
// Fire-and-forget
await scheduler.EnqueueAsync<SendWelcomeEmailJob, WelcomeEmailArgs>(new WelcomeEmailArgs { UserId = userId });

// Delayed
await scheduler.ScheduleAsync<GenerateInvoiceJob>(delay: TimeSpan.FromHours(24));

// Recurring
await scheduler.AddOrUpdateRecurringAsync<CleanupJob>("daily-cleanup", "0 2 * * *");
```

## Custom Store (Production)

```csharp
builder.Services
    .AddPrimitivesJobs()
    .AddJobStore<MyDatabaseJobStore>();
```
