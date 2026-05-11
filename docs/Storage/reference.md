---
layout: default
library: storage
title: Configuration Reference
description: Full options reference for Primitives.Storage — all types, properties, and defaults.
permalink: /storage/reference/
---

## DI extension methods

### `AddPrimitivesStorage`

```csharp
IServiceCollection AddPrimitivesStorage(
    this IServiceCollection services,
    Action<StorageOptions>? configure = null)
```

Registers `IStorageService` as a singleton backed by the **local filesystem** provider.
Safe to call multiple times (uses `TryAddSingleton`).

### `AddPrimitivesStorageAzure` (options-based)

```csharp
IServiceCollection AddPrimitivesStorageAzure(
    this IServiceCollection services,
    Action<AzureBlobStorageOptions> configureAzure,
    Action<StorageOptions>? configureStorage = null)
```

Registers `IStorageService` backed by **Azure Blob Storage**. Creates a `BlobServiceClient`
from `AzureBlobStorageOptions`. Requires either `ConnectionString` or `AccountName` + `AccountKey`.

### `AddPrimitivesStorageAzure` (client-based)

```csharp
IServiceCollection AddPrimitivesStorageAzure(
    this IServiceCollection services,
    BlobServiceClient client,
    Action<AzureBlobStorageOptions>? configureAzure = null,
    Action<StorageOptions>? configureStorage = null)
```

Registers `IStorageService` backed by Azure Blob Storage using a pre-built `BlobServiceClient`.
Use this overload for **managed identity**, `DefaultAzureCredential`, or custom client configurations.

---

## `IStorageService`

```csharp
public interface IStorageService
{
    Task UploadAsync(string bucket, string objectName, Stream content,
        UploadOptions? options = null, CancellationToken ct = default);

    Task<Stream> DownloadAsync(string bucket, string objectName,
        DownloadOptions? options = null, CancellationToken ct = default);

    Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default);

    Task<bool> ExistsAsync(string bucket, string objectName, CancellationToken ct = default);

    Task<StorageObject?> GetMetadataAsync(string bucket, string objectName,
        CancellationToken ct = default);

    Task<StorageObjectList> ListAsync(string bucket, ListOptions? options = null,
        CancellationToken ct = default);

    Task<Uri> GetSignedUrlAsync(string bucket, string objectName,
        SignedUrlOptions options, CancellationToken ct = default);
}
```

---

## `StorageOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BasePath` | `string` | `Path.GetTempPath()` | Root directory for the local filesystem provider. |
| `MaxUploadSizeBytes` | `long` | `104,857,600` (100 MB) | Maximum allowed upload size for local provider. |

---

## `UploadOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ContentType` | `string?` | `null` | MIME type (e.g. `"image/jpeg"`). |
| `Metadata` | `IDictionary<string,string>?` | `null` | User-defined key/value metadata. |
| `CacheControl` | `string?` | `null` | HTTP Cache-Control header value. |
| `ContentEncoding` | `string?` | `null` | HTTP Content-Encoding header value. |

---

## `DownloadOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FromByte` | `long?` | `null` | Start byte offset (0-based, inclusive). `null` = beginning of object. |
| `ToByte` | `long?` | `null` | End byte offset (0-based, inclusive). `null` = end of object. |

---

## `ListOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Prefix` | `string?` | `null` | Only return objects whose names start with this value. |
| `MaxResults` | `int` | `100` | Maximum objects per page. |
| `ContinuationToken` | `string?` | `null` | Opaque token from previous `StorageObjectList.ContinuationToken`. |

---

## `SignedUrlOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Expiry` | `TimeSpan` | `1 hour` | How long the URL remains valid. |
| `Verb` | `SignedUrlVerb` | `Get` | HTTP verb the URL permits. |

### `SignedUrlVerb` enum

| Value | HTTP verb | Azure SAS permission |
|-------|-----------|----------------------|
| `Get` | GET | `Read` |
| `Put` | PUT | `Write \| Create` |
| `Delete` | DELETE | `Delete` |

---

## `StorageObject`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Object name within its bucket. |
| `SizeBytes` | `long` | Object size in bytes. |
| `ContentType` | `string?` | MIME type, if known. |
| `ETag` | `string?` | Entity tag, if provided by the backend. |
| `LastModified` | `DateTimeOffset?` | Last modification time, if known. |
| `Metadata` | `IReadOnlyDictionary<string,string>` | User-defined metadata. Empty dictionary if none. |

---

## `StorageObjectList`

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<StorageObject>` | Objects in this page. |
| `ContinuationToken` | `string?` | Token for the next page; `null` when no further pages. |

---

## `AzureBlobStorageOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string?` | `null` | Azure Storage connection string. Takes precedence over `AccountName`/`AccountKey`. |
| `AccountName` | `string?` | `null` | Storage account name. |
| `AccountKey` | `string?` | `null` | Storage account key. |
| `CreateContainersIfNotExist` | `bool` | `true` | Automatically create containers on first write. |

---

## Exceptions

| Exception | When thrown |
|-----------|-------------|
| `StorageException` | Object not found on download; other domain-level storage errors. |
| `NotSupportedException` | `GetSignedUrlAsync` called on local provider, or on Azure without a shared-key credential. |
| `InvalidOperationException` | Azure options missing both `ConnectionString` and `AccountName`/`AccountKey`. |
