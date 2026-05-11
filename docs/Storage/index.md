---
layout: default
library: storage
permalink: /storage/
---

<div class="bd-hero">
  <h1>Primitives.Storage</h1>
  <p class="lead">
    Unified abstraction over Azure Blob, S3, and local filesystem with streaming,
    signed URLs, and object lifecycle management. One interface — swap providers
    without changing application code.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Storage
  </div>
</div>

## The problem it solves

Storage SDK APIs differ significantly between Azure Blob, S3, and the local filesystem.
Code written against `BlobClient` cannot be tested without Azure infrastructure, and switching
providers requires rewriting every call site.

`Primitives.Storage` gives you:

- **One interface** — `IStorageService` — covering upload, download, delete, exists, list, metadata, and signed URLs.
- **Local filesystem provider** — fully functional, zero-dependency, perfect for development and unit testing.
- **Azure Blob Storage provider** — `Primitives.Storage.Azure` wraps the official Azure SDK without leaking it into application code.
- **Swap at the DI root** — switch from local to Azure by changing one registration; no application code changes.

## Quick start

```csharp
// Program.cs — local filesystem (development)
builder.Services.AddPrimitivesStorage(o =>
{
    o.BasePath = "/var/data/blobs";
});

// Program.cs — Azure Blob Storage (production)
builder.Services.AddPrimitivesStorageAzure(azure =>
{
    azure.ConnectionString = builder.Configuration["Storage:ConnectionString"];
});
```

```csharp
// Upload and download — same code regardless of provider
public class AvatarService(IStorageService storage)
{
    public Task SaveAsync(int id, Stream img, CancellationToken ct) =>
        storage.UploadAsync("avatars", $"user/{id}.jpg", img,
            new UploadOptions { ContentType = "image/jpeg" }, ct);

    public Task<Stream> GetAsync(int id, CancellationToken ct) =>
        storage.DownloadAsync("avatars", $"user/{id}.jpg", ct: ct);
}
```

## Packages

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Storage</div>
    <p>Core <code>IStorageService</code> abstraction and local filesystem provider. Zero cloud dependencies — ideal for development and CI testing.</p>
    <div class="install-cmd">dotnet add package Primitives.Storage</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Storage.Azure</div>
    <p>Azure Blob Storage provider. Supports connection strings, shared-key credentials, and managed identity via the BlobServiceClient overload.</p>
    <div class="install-cmd">dotnet add package Primitives.Storage.Azure</div>
  </div>
</div>

## Core concepts

**Buckets and object names.** Every operation is scoped to a `bucket` (container in Azure, directory in local)
and an `objectName`. Object names may contain forward slashes to express a virtual folder hierarchy
(`"images/2024/photo.jpg"`).

**Provider-agnostic streaming.** `UploadAsync` accepts any `Stream`; `DownloadAsync` returns an owned
`Stream` — no byte arrays, no buffering the whole object in memory.

**Signed URLs.** Cloud providers can generate time-limited pre-signed URLs (`GetSignedUrlAsync`).
The local provider throws `NotSupportedException`, making tests that accidentally call this method fail fast.
