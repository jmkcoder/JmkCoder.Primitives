# Primitives.RateLimiting

Server-side rate limiting for multi-tenant SaaS. Sliding-window and token-bucket algorithms with per-tenant quotas, pluggable counter stores, and ASP.NET Core middleware.

## Quick Start

```csharp
builder.Services.AddPrimitivesRateLimiting(opts =>
{
    opts.Policies.Add(new RateLimitPolicy
    {
        Name        = "api",
        PermitLimit = 100,
        Window      = TimeSpan.FromMinutes(1),
        Algorithm   = RateLimitAlgorithm.SlidingWindow
    });
});

// In the pipeline
app.UsePrimitivesRateLimiting("api");
```

## Per-Tenant Key

```csharp
builder.Services
    .AddPrimitivesRateLimiting(...)
    .AddKeyProvider<TenantRateLimitKeyProvider>(); // implements IRateLimitKeyProvider
```

## Programmatic Check

```csharp
var result = await rateLimiter.AcquireAsync("api", tenantId);
if (!result.IsAllowed)
    return Results.StatusCode(429);
```
