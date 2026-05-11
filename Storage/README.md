# Primitives.Storage

Unified abstraction over Azure Blob, S3, and local filesystem storage with streaming, signed URLs, and object lifecycle management.

## Packages

| Package | Description |
|---------|-------------|
| `Primitives.Storage` | Core `IStorageService` abstraction + fully functional local filesystem provider for development and testing |
| `Primitives.Storage.Azure` | Azure Blob Storage provider with connection string, shared-key, and managed-identity support |

## Quick start

```bash
dotnet add package Primitives.Storage          # local filesystem (dev/test)
dotnet add package Primitives.Storage.Azure    # Azure Blob Storage (production)
```

### Local filesystem (development / testing)

```csharp
// Program.cs
builder.Services.AddPrimitivesStorage(o =>
{
    o.BasePath = "/var/data/storage";  // defaults to system temp dir
});
```

### Azure Blob Storage

```csharp
// Program.cs — connection string
builder.Services.AddPrimitivesStorageAzure(azure =>
{
    azure.ConnectionString = builder.Configuration["Storage:ConnectionString"];
});

// Program.cs — managed identity (bring your own BlobServiceClient)
builder.Services.AddPrimitivesStorageAzure(
    new BlobServiceClient(
        new Uri("https://myaccount.blob.core.windows.net"),
        new DefaultAzureCredential()));
```

### Inject and use

```csharp
public class AvatarService(IStorageService storage)
{
    public async Task SaveAsync(int userId, Stream image, CancellationToken ct)
    {
        await storage.UploadAsync("avatars", $"user/{userId}.jpg", image,
            new UploadOptions { ContentType = "image/jpeg" }, ct);
    }

    public Task<Stream> GetAsync(int userId, CancellationToken ct) =>
        storage.DownloadAsync("avatars", $"user/{userId}.jpg", ct: ct);
}
```

## Operations

| Method | Description |
|--------|-------------|
| `UploadAsync` | Upload a stream, replacing any existing object |
| `DownloadAsync` | Open a stream to the object (caller disposes) |
| `DeleteAsync` | Delete an object (no-op if not found) |
| `ExistsAsync` | Check whether an object exists |
| `GetMetadataAsync` | Retrieve size, content-type, ETag, last-modified, and custom metadata |
| `ListAsync` | List objects with optional prefix filter and pagination |
| `GetSignedUrlAsync` | Generate a time-limited pre-signed URL (cloud providers only) |

## Signed URLs

```csharp
var url = await storage.GetSignedUrlAsync("avatars", "user/42.jpg", new SignedUrlOptions
{
    Expiry = TimeSpan.FromMinutes(15),
    Verb   = SignedUrlVerb.Get,
});
// → https://myaccount.blob.core.windows.net/avatars/user/42.jpg?sv=…
```

> The local filesystem provider throws `NotSupportedException` for signed URLs — swap in the Azure provider for signed URL support.

## License

MIT
