---
layout: default
library: storage
title: Operations
description: All IStorageService methods — upload, download, delete, exists, metadata, list, and signed URLs.
permalink: /storage/operations/
---

## Upload

```csharp
await storage.UploadAsync(
    bucket:     "avatars",
    objectName: "user/42.jpg",
    content:    imageStream,
    options: new UploadOptions
    {
        ContentType     = "image/jpeg",
        CacheControl    = "public, max-age=31536000",
        ContentEncoding = null,
        Metadata        = new Dictionary<string, string>
        {
            ["uploaded-by"] = userId.ToString(),
            ["source"]      = "web-upload",
        },
    });
```

- **Replaces** any existing object with the same name.
- The `Stream` is read once; it does not need to be seekable.
- `UploadOptions` is optional — pass `null` to use provider defaults.

---

## Download

```csharp
// Full download
await using var stream = await storage.DownloadAsync("avatars", "user/42.jpg");
await stream.CopyToAsync(responseStream);

// Partial download (byte range)
await using var partial = await storage.DownloadAsync("videos", "clip.mp4", new DownloadOptions
{
    FromByte = 1_048_576,  // start at 1 MB
    ToByte   = 2_097_151,  // end at 2 MB - 1 byte
});
```

<div class="bd-callout bd-callout-warning">
<strong>Dispose the stream.</strong> <code>DownloadAsync</code> returns an owned <code>Stream</code>. Always
wrap in <code>await using</code> or dispose explicitly to release the underlying connection or file handle.
</div>

**Exception:** `StorageException` is thrown if the object does not exist.

---

## Delete

```csharp
await storage.DeleteAsync("avatars", "user/42.jpg");
```

- No-op if the object does not exist — no exception is thrown.

---

## Exists

```csharp
bool exists = await storage.ExistsAsync("avatars", "user/42.jpg");
```

---

## GetMetadata

Returns a `StorageObject` with name, size, content-type, ETag, last-modified date, and custom metadata.
Returns `null` if the object does not exist.

```csharp
StorageObject? meta = await storage.GetMetadataAsync("avatars", "user/42.jpg");
if (meta is not null)
{
    Console.WriteLine($"{meta.Name}: {meta.SizeBytes} bytes, {meta.ContentType}");
    Console.WriteLine($"Last modified: {meta.LastModified}");
}
```

---

## List

```csharp
// All objects in a bucket
var page = await storage.ListAsync("documents");

// Filtered by prefix
var images = await storage.ListAsync("assets", new ListOptions
{
    Prefix     = "images/2024/",
    MaxResults = 50,
});

// Pagination
string? token = null;
do
{
    var result = await storage.ListAsync("files", new ListOptions
    {
        MaxResults        = 100,
        ContinuationToken = token,
    });
    ProcessPage(result.Items);
    token = result.ContinuationToken;
}
while (token is not null);
```

`ListAsync` returns a `StorageObjectList` with `Items` and an opaque `ContinuationToken` that is
`null` when there are no further pages.

---

## Signed URLs

```csharp
// 15-minute read URL
Uri url = await storage.GetSignedUrlAsync("uploads", "report.pdf", new SignedUrlOptions
{
    Expiry = TimeSpan.FromMinutes(15),
    Verb   = SignedUrlVerb.Get,
});

// 5-minute upload URL (client uploads directly to storage)
Uri uploadUrl = await storage.GetSignedUrlAsync("uploads", $"pending/{Guid.NewGuid()}.bin",
    new SignedUrlOptions
    {
        Expiry = TimeSpan.FromMinutes(5),
        Verb   = SignedUrlVerb.Put,
    });
```

| `SignedUrlVerb` | Permitted operation |
|----------------|---------------------|
| `Get` (default) | Read / download |
| `Put` | Write / upload |
| `Delete` | Delete |

<div class="bd-callout bd-callout-info">
The local filesystem provider throws <code>NotSupportedException</code> for <code>GetSignedUrlAsync</code>.
This ensures tests that rely on signed URLs fail fast and are not silently skipped.
</div>
