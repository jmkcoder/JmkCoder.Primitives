using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Storage.Abstractions;
using Primitives.Storage.Exceptions;
using Primitives.Storage.Models;

namespace Primitives.Storage.Internal;

/// <summary>
/// Local filesystem implementation of <see cref="IStorageService"/>.
/// Buckets map to directories under <see cref="StorageOptions.BasePath"/>.
/// Object metadata (content-type, custom metadata) is persisted in a hidden
/// <c>.primitives</c> subdirectory alongside the data files.
/// </summary>
internal sealed class LocalStorageService : IStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly StorageOptions _options;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IOptions<StorageOptions> options, ILogger<LocalStorageService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    // ── Upload ───────────────────────────────────────────────────────────────

    public async Task UploadAsync(string bucket, string objectName, Stream content,
        UploadOptions? options = null, CancellationToken ct = default)
    {
        var filePath = GetFilePath(bucket, objectName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize: 81_920, useAsync: true);
        await content.CopyToAsync(fs, ct);

        if (options != null)
            await WriteSidecarAsync(bucket, objectName, MapToSidecar(options), ct);

        _logger.LogDebug("Uploaded {ObjectName} to local bucket {Bucket}", objectName, bucket);
    }

    // ── Download ─────────────────────────────────────────────────────────────

    public Task<Stream> DownloadAsync(string bucket, string objectName,
        DownloadOptions? options = null, CancellationToken ct = default)
    {
        var filePath = GetFilePath(bucket, objectName);
        if (!File.Exists(filePath))
            throw new StorageException($"Object '{objectName}' not found in bucket '{bucket}'.");

        var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81_920, useAsync: true);

        if (options?.FromByte is { } fromByte && fromByte > 0)
            fs.Seek(fromByte, SeekOrigin.Begin);

        return Task.FromResult<Stream>(fs);
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    public Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var filePath = GetFilePath(bucket, objectName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        var metaPath = GetMetaPath(bucket, objectName);
        if (File.Exists(metaPath))
            File.Delete(metaPath);

        return Task.CompletedTask;
    }

    // ── Exists ───────────────────────────────────────────────────────────────

    public Task<bool> ExistsAsync(string bucket, string objectName, CancellationToken ct = default)
        => Task.FromResult(File.Exists(GetFilePath(bucket, objectName)));

    // ── GetMetadata ──────────────────────────────────────────────────────────

    public async Task<StorageObject?> GetMetadataAsync(string bucket, string objectName,
        CancellationToken ct = default)
    {
        var filePath = GetFilePath(bucket, objectName);
        if (!File.Exists(filePath))
            return null;

        var info    = new FileInfo(filePath);
        var sidecar = await ReadSidecarAsync(bucket, objectName, ct);

        return new StorageObject
        {
            Name         = objectName,
            SizeBytes    = info.Length,
            ContentType  = sidecar?.ContentType,
            LastModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            Metadata     = sidecar?.Metadata?.AsReadOnly()
                           ?? (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(),
        };
    }

    // ── List ─────────────────────────────────────────────────────────────────

    public Task<StorageObjectList> ListAsync(string bucket, ListOptions? options = null,
        CancellationToken ct = default)
    {
        var bucketPath = Path.Combine(_options.BasePath, bucket);
        if (!Directory.Exists(bucketPath))
            return Task.FromResult(new StorageObjectList());

        var prefix     = options?.Prefix ?? string.Empty;
        var maxResults = options?.MaxResults ?? 100;
        var skip       = options?.ContinuationToken;

        var metaSep = Path.DirectorySeparatorChar + ".primitives" + Path.DirectorySeparatorChar;

        var allNames = Directory
            .EnumerateFiles(bucketPath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(bucketPath, f).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(name => !name.Contains("/.primitives/"))
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Apply continuation token (resume after the given name)
        var startIndex = skip is null ? 0 : allNames.IndexOf(skip);
        if (startIndex < 0) startIndex = 0;

        var page = allNames.Skip(startIndex).Take(maxResults).ToList();
        var nextToken = (startIndex + maxResults) < allNames.Count
            ? allNames[startIndex + maxResults]
            : null;

        var items = page.Select(name =>
        {
            var info = new FileInfo(
                Path.Combine(bucketPath, name.Replace('/', Path.DirectorySeparatorChar)));
            return new StorageObject
            {
                Name         = name,
                SizeBytes    = info.Exists ? info.Length : 0,
                LastModified = info.Exists
                    ? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero)
                    : null,
            };
        }).ToList();

        return Task.FromResult(new StorageObjectList
        {
            Items             = items,
            ContinuationToken = nextToken,
        });
    }

    // ── Signed URL ───────────────────────────────────────────────────────────

    public Task<Uri> GetSignedUrlAsync(string bucket, string objectName,
        SignedUrlOptions options, CancellationToken ct = default)
        => throw new NotSupportedException(
            "Signed URLs are not supported by the local filesystem storage provider. " +
            "Use Primitives.Storage.Azure or another cloud provider for signed URL support.");

    // ── Private helpers ──────────────────────────────────────────────────────

    private string GetFilePath(string bucket, string objectName) =>
        Path.Combine(_options.BasePath, bucket,
            objectName.Replace('/', Path.DirectorySeparatorChar));

    private string GetMetaPath(string bucket, string objectName) =>
        Path.Combine(_options.BasePath, bucket, ".primitives",
            objectName.Replace('/', Path.DirectorySeparatorChar) + ".meta.json");

    private async Task WriteSidecarAsync(string bucket, string objectName,
        MetadataSidecar sidecar, CancellationToken ct)
    {
        var metaPath = GetMetaPath(bucket, objectName);
        Directory.CreateDirectory(Path.GetDirectoryName(metaPath)!);

        await using var fs = new FileStream(metaPath, FileMode.Create, FileAccess.Write,
            FileShare.None, 4_096, useAsync: true);
        await JsonSerializer.SerializeAsync(fs, sidecar, JsonOptions, ct);
    }

    private async Task<MetadataSidecar?> ReadSidecarAsync(string bucket, string objectName,
        CancellationToken ct)
    {
        var metaPath = GetMetaPath(bucket, objectName);
        if (!File.Exists(metaPath))
            return null;

        await using var fs = new FileStream(metaPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4_096, useAsync: true);
        return await JsonSerializer.DeserializeAsync<MetadataSidecar>(fs, JsonOptions, ct);
    }

    private static MetadataSidecar MapToSidecar(UploadOptions options) => new()
    {
        ContentType     = options.ContentType,
        CacheControl    = options.CacheControl,
        ContentEncoding = options.ContentEncoding,
        Metadata        = options.Metadata?.ToDictionary(k => k.Key, v => v.Value),
    };
}
