---
layout: default
library: authentication
title: Health Checks
description: The built-in health check calls CanHandleAsync on every registered strategy and reports degraded or unhealthy when any strategy is unreachable.
permalink: /authentication/health-checks/
---

## What the health check does — and why

A health check endpoint (`/healthz`) tells infrastructure (Kubernetes, Azure App Service, load
balancers, monitoring tools) whether your service is ready to handle traffic.

For an authentication service, "healthy" means: the strategies are configured correctly and can
reach their dependencies (the OIDC authority, the Kerberos KDC, the user database). If a strategy
is misconfigured or its dependency is unreachable, incoming authentication requests will fail —
but the failure might not be obvious until a user tries to log in.

The Primitives health check surfaces this proactively by calling `CanHandleAsync()` on each
registered strategy on a polling schedule (typically every 30 seconds). This is not a full
authentication round-trip — `CanHandleAsync()` is designed to be a cheap connectivity check:

- **OIDC**: fetch the OIDC discovery document (`{Authority}/.well-known/openid-configuration`)
- **Kerberos**: verify the SPN is resolvable and the GSSAPI library is available
- **Username/Password**: verify the options are non-empty (always fast, no I/O)
- **API Key**: verify the key is non-empty (always fast, no I/O)
- **Custom**: implement whatever lightweight check makes sense for your strategy

This maps cleanly to Kubernetes readiness probes: if authentication becomes unavailable, the pod
should be removed from the load balancer until it recovers.

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