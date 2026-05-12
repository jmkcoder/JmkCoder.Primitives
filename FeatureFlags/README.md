# Primitives.FeatureFlags

Per-tenant feature flag evaluation with pluggable stores. Supports static flags, per-tenant overrides, and percentage rollouts.

## Quick Start

```csharp
builder.Services.AddPrimitivesFeatureFlags(opts =>
{
    opts.Flags.Add(new FeatureFlag { Name = "new-dashboard", IsEnabled = false, RolloutPercentage = 20 });
    opts.Flags.Add(new FeatureFlag { Name = "billing-v2", IsEnabled = true });
});
```

## Evaluation

```csharp
// Global
bool enabled = await featureFlags.IsEnabledAsync("new-dashboard");

// Per-tenant (overrides global)
bool enabled = await featureFlags.IsEnabledForTenantAsync("new-dashboard", tenantId);

// Per-subject with rollout (deterministic bucket hash)
bool enabled = await featureFlags.IsEnabledForSubjectAsync("new-dashboard", userId, tenantId);
```

## Custom Store

```csharp
builder.Services
    .AddPrimitivesFeatureFlags()
    .AddFeatureFlagStore<MyDatabaseFlagStore>();
```
