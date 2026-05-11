---
layout: default
library: storage
title: Installation
description: Add Primitives.Storage to a .NET 8 project and register the local or Azure provider.
permalink: /storage/getting-started/
---

## Requirements

- .NET 8 or later

## Install the core package

```bash
dotnet add package Primitives.Storage
```

This is enough for the local filesystem provider, which covers development, integration tests, and any scenario that does not require cloud storage.

## Register the local filesystem provider

```csharp
// Program.cs
builder.Services.AddPrimitivesStorage(o =>
{
    // Defaults to Path.GetTempPath() — override for a stable path
    o.BasePath = builder.Configuration["Storage:LocalPath"]
                 ?? Path.Combine(AppContext.BaseDirectory, "blobs");
});
```

## Add Azure Blob Storage

```bash
dotnet add package Primitives.Storage.Azure
```

### Connection string (most common)

```csharp
builder.Services.AddPrimitivesStorageAzure(azure =>
{
    azure.ConnectionString = builder.Configuration["Storage:ConnectionString"];
});
```

### Account name + key

```csharp
builder.Services.AddPrimitivesStorageAzure(azure =>
{
    azure.AccountName = builder.Configuration["Storage:AccountName"];
    azure.AccountKey  = builder.Configuration["Storage:AccountKey"];
});
```

### Managed identity (DefaultAzureCredential)

```bash
dotnet add package Azure.Identity
```

```csharp
builder.Services.AddPrimitivesStorageAzure(
    new BlobServiceClient(
        new Uri($"https://{accountName}.blob.core.windows.net"),
        new DefaultAzureCredential()));
```

## Switch providers by environment

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddPrimitivesStorage(o =>
        o.BasePath = Path.Combine(AppContext.BaseDirectory, "dev-blobs"));
}
else
{
    builder.Services.AddPrimitivesStorageAzure(azure =>
        azure.ConnectionString = builder.Configuration["Storage:ConnectionString"]);
}
```

Application code depends only on `IStorageService` — no provider-specific code at call sites.

## Inject and use

```csharp
public class DocumentService(IStorageService storage, ILogger<DocumentService> logger)
{
    private const string Bucket = "documents";

    public async Task<string> StoreAsync(string name, Stream content, CancellationToken ct)
    {
        var objectName = $"{Guid.NewGuid():N}/{name}";
        await storage.UploadAsync(Bucket, objectName, content,
            new UploadOptions { ContentType = "application/pdf" }, ct);
        logger.LogInformation("Stored document {Name} as {Object}", name, objectName);
        return objectName;
    }

    public Task<Stream> OpenAsync(string objectName, CancellationToken ct) =>
        storage.DownloadAsync(Bucket, objectName, ct: ct);
}
```

## Next steps

- [Operations]({{ '/storage/operations/' | relative_url }}) — all `IStorageService` methods with examples
- [Providers]({{ '/storage/providers/' | relative_url }}) — local filesystem vs Azure in detail
- [Configuration Reference]({{ '/storage/reference/' | relative_url }}) — all options
