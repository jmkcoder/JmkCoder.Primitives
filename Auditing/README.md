# Primitives.Auditing

Structured, queryable audit logging for multi-tenant SaaS. GDPR- and SOC-2-ready event model with pluggable stores.

## Quick Start

```csharp
builder.Services.AddPrimitivesAuditing();
```

## Recording Events

```csharp
await auditLogger.LogAsync(new AuditEvent
{
    ActorId      = userId,
    ActorName    = userName,
    Action       = "invoice.created",
    ResourceType = "invoice",
    ResourceId   = invoiceId,
    TenantId     = tenantId,
    Outcome      = AuditOutcome.Success,
    IpAddress    = httpContext.Connection.RemoteIpAddress?.ToString(),
});
```

## Querying Events

```csharp
var result = await auditLogger.QueryAsync(new AuditQuery
{
    TenantId  = tenantId,
    Action    = "invoice.deleted",
    From      = DateTimeOffset.UtcNow.AddDays(-30),
    PageSize  = 20,
    Page      = 0,
});
```

## Custom Store (Production)

```csharp
builder.Services
    .AddPrimitivesAuditing()
    .AddAuditStore<MyDatabaseAuditStore>();
```
