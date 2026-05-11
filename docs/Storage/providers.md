---
layout: default
library: storage
title: Providers
description: Local filesystem and Azure Blob Storage providers — differences, capabilities, and when to use each.
permalink: /storage/providers/
---

## Local filesystem provider

Included in `Primitives.Storage`. Zero cloud dependencies. All data is written to the local filesystem under `StorageOptions.BasePath`.

```csharp
services.AddPrimitivesStorage(o =>
{
    o.BasePath = "/var/data/blobs";
});
```

### How it works

| Concept | Local mapping |
|---------|---------------|
| Bucket | Subdirectory under `BasePath` (`{BasePath}/{bucket}/`) |
| Object name | File path relative to bucket, with `/` as separator (`images/cat.jpg` → `{bucket}/images/cat.jpg`) |
| Metadata | Stored in a hidden `.primitives/` subdirectory alongside each file as a `.meta.json` sidecar |
| Listing | `Directory.EnumerateFiles` with optional prefix filter |

### Capabilities

| Operation | Supported |
|-----------|-----------|
| Upload | ✅ |
| Download | ✅ with optional `FromByte` range |
| Delete | ✅ (no-op if not found) |
| Exists | ✅ |
| GetMetadata | ✅ (content-type and custom metadata from sidecar) |
| List | ✅ with prefix and pagination |
| Signed URLs | ❌ throws `NotSupportedException` |

### Recommended uses

- Local development — no Azure account needed
- Unit and integration tests — create a temp directory per test, delete on teardown
- CI/CD pipelines — no credentials required
- Air-gapped environments

### Example: using in tests

```csharp
public sealed class MyServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "test-" + Guid.NewGuid().ToString("N"));
    private readonly IStorageService _storage;

    public MyServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        var sp = new ServiceCollection()
            .AddPrimitivesStorage(o => o.BasePath = _tempDir)
            .BuildServiceProvider();
        _storage = sp.GetRequiredService<IStorageService>();
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
```

---

## Azure Blob Storage provider

Installed separately via `Primitives.Storage.Azure`. Wraps the official `Azure.Storage.Blobs` SDK.

### Registration options

#### Connection string

```csharp
services.AddPrimitivesStorageAzure(azure =>
{
    azure.ConnectionString = configuration["Storage:ConnectionString"];
});
```

#### Shared-key credential

```csharp
services.AddPrimitivesStorageAzure(azure =>
{
    azure.AccountName = configuration["Storage:AccountName"];
    azure.AccountKey  = configuration["Storage:AccountKey"];
});
```

#### Managed identity / DefaultAzureCredential

```csharp
services.AddPrimitivesStorageAzure(
    new BlobServiceClient(
        new Uri($"https://{accountName}.blob.core.windows.net"),
        new DefaultAzureCredential()));
```

### How it works

| Concept | Azure mapping |
|---------|---------------|
| Bucket | Blob container |
| Object name | Blob name (supports `/` for virtual folder hierarchy) |
| Metadata | Native Azure blob metadata and HTTP headers |
| Listing | `BlobContainerClient.GetBlobsAsync` |
| Signed URLs | `BlobSasBuilder` with `BlobClient.GenerateSasUri` |

### Capabilities

| Operation | Supported |
|-----------|-----------|
| Upload | ✅ streaming, with content-type, cache-control, and metadata |
| Download | ✅ streaming, with byte-range support |
| Delete | ✅ (`DeleteIfExistsAsync` — no-op if not found) |
| Exists | ✅ |
| GetMetadata | ✅ |
| List | ✅ with prefix filter (pagination support coming) |
| Signed URLs (GET) | ✅ requires shared-key credential or connection string |
| Signed URLs (PUT/DELETE) | ✅ |
| Signed URLs (managed identity) | ❌ SAS requires shared-key; use User Delegation Keys directly |

### Auto-creating containers

By default, `AddPrimitivesStorageAzure` creates blob containers on first write.
Disable this if containers are pre-provisioned:

```csharp
services.AddPrimitivesStorageAzure(azure =>
{
    azure.ConnectionString           = "…";
    azure.CreateContainersIfNotExist = false;
});
```

### Signed URL requirements

`GetSignedUrlAsync` requires the `BlobServiceClient` to have been created with a
`StorageSharedKeyCredential` (connection string or account name + key). With managed
identity, `BlobClient.CanGenerateSasUri` returns `false` and the method throws
`NotSupportedException`. For managed-identity signed URLs, generate User Delegation Keys
directly via the Azure SDK.
