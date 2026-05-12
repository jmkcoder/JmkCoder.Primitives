# Primitives.Authorization

Role-based and permission-based authorization for multi-tenant SaaS. Supports per-tenant roles, permission sets, and resource-level access control.

## Quick Start

```csharp
builder.Services.AddPrimitivesAuthorization(opts =>
{
    opts.Roles.Add(new Role
    {
        Name = "admin",
        TenantId = "tenant-1",
        Permissions = new HashSet<string> { "invoices:read", "invoices:write", "users:manage" }
    });
});
```

## Checking Permissions

```csharp
// Tenant-level
bool canRead = await permissions.HasPermissionAsync(userId, "invoices:read", tenantId);

// Resource-level (falls back to tenant-level)
bool canEdit = await permissions.HasPermissionOnResourceAsync(userId, "documents:edit", "document", docId, tenantId);

// Get all permissions
IReadOnlySet<string> all = await permissions.GetPermissionsAsync(userId, tenantId);
```

## Assigning Roles at Runtime

```csharp
await permissionStore.AssignRoleAsync(userId, "admin", tenantId);
await permissionStore.RevokeRoleAsync(userId, "viewer", tenantId);
```

## Custom Stores

```csharp
builder.Services
    .AddPrimitivesAuthorization()
    .AddRoleStore<MyDatabaseRoleStore>()
    .AddPermissionStore<MyDatabasePermissionStore>();
```
