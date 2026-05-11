---
layout: default
title: Health Checks
description: The built-in health check calls CanHandleAsync on every registered strategy and reports degraded or unhealthy when any strategy is unreachable.
permalink: /health-checks/
---

## Registration

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc(o => { … })
    .AddJwtTokenIssuance(o => { … })
    .AddHealthCheck(
        name:          "authentication",      // default
        failureStatus: HealthStatus.Degraded, // default
        tags:          ["ready", "auth"]);
```

Then expose the health endpoint:

```csharp
app.MapHealthChecks("/healthz");

// Or with a filtered endpoint for readiness probes
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

---

## What the check does

The health check calls `IAuthenticationStrategy.CanHandleAsync()` on each registered strategy. The aggregate result:

| Outcome | HTTP status | Reported status |
|---|---|---|
| All strategies healthy | `200 OK` | `Healthy` |
| One or more degraded | `200 OK` (configurable) | `Degraded` |
| Exception during check | `503 Service Unavailable` | `Unhealthy` |

<div class="bd-callout bd-callout-tip">
<strong>Keep <code>CanHandleAsync</code> lightweight.</strong> It is called on a polling schedule.
Use it to verify connectivity to the identity provider (a cheap discovery endpoint ping) rather
than performing a full authentication round-trip.
</div>

---

## Customising failure status

```csharp
// Report as Unhealthy (not Degraded) when a strategy is unreachable
.AddHealthCheck(failureStatus: HealthStatus.Unhealthy)
```

This controls the `HealthCheckResult` returned by the check. Whether that maps to HTTP 200 or 503 is controlled by `HealthCheckOptions.ResultStatusCodes` in `MapHealthChecks`.

---

## Example health response

With the `Microsoft.Extensions.Diagnostics.HealthChecks` JSON writer:

```json
{
  "status": "Degraded",
  "results": {
    "authentication": {
      "status": "Degraded",
      "description": "OIDC strategy cannot reach https://login.microsoftonline.com",
      "data": {}
    }
  }
}
```

---

## Multiple named strategies

The health check iterates all registered `IAuthenticationStrategy` instances. If you have three strategies registered, all three are checked and the worst result is reported.

---

## `AddHealthCheck` signature

```csharp
AuthenticationBuilder AddHealthCheck(
    string       name          = "authentication",
    HealthStatus failureStatus = HealthStatus.Degraded,
    params string[] tags)
```

| Parameter | Default | Description |
|---|---|---|
| `name` | `"authentication"` | Name shown in health check results and used for tag filtering |
| `failureStatus` | `Degraded` | Reported status when `CanHandleAsync` returns `false` |
| `tags` | `[]` | Tags for health check filtering |
