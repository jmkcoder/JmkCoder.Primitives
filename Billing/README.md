# Primitives.Billing

Usage metering and entitlement enforcement for SaaS. Records feature consumption against plan-based quotas with pluggable stores.

## Quick Start

```csharp
builder.Services.AddPrimitivesBilling();
```

## Configuring Entitlements

```csharp
// Seed via the store at startup
await entitlementStore.UpsertAsync(new Entitlement
{
    TenantId  = "tenant-1",
    Feature   = "api-calls",
    Limit     = 10_000,
    PlanName  = "Pro",
});
```

## Metering Usage

```csharp
// Check before allowing
if (!await entitlements.IsAllowedAsync(tenantId, "api-calls"))
    return Results.StatusCode(402); // Payment Required

// Record after use
await usageMeter.RecordAsync(tenantId, "api-calls");
```

## Querying Usage

```csharp
decimal used = await usageMeter.GetUsageAsync(tenantId, "api-calls");
Entitlement? plan = await entitlements.GetEntitlementAsync(tenantId, "api-calls");
```

## Custom Stores (Production)

```csharp
builder.Services
    .AddPrimitivesBilling()
    .AddEntitlementStore<MyDatabaseEntitlementStore>()
    .AddUsageStore<MyDatabaseUsageStore>();
```
