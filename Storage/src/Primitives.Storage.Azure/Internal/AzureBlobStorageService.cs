using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Storage.Abstractions;
using Primitives.Storage.Models;

namespace Primitives.Storage.Azure.Internal;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IStorageService"/>.
/// Buckets map to blob containers; object names map directly to blob names
/// (forward slashes create virtual folder hierarchy).
/// </summary>
internal sealed class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _client;
    private readonly AzureBlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient client,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _client  = client;
        _options = options.Value;
        _logger  = logger;
    }

    // ── Upload ───────────────────────────────────────────────────────────────

    public async Task UploadAsync(string bucket, string objectName, Stream content,
        UploadOptions? options = null, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(bucket);
        if (_options.CreateContainersIfNotExist)
            await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob          = container.GetBlobClient(objectName);
        var uploadOptions = new BlobUploadOptions();

        if (options != null)
        {
            uploadOptions.HttpHeaders = new BlobHttpHeaders
            {
                ContentType     = options.ContentType,
                CacheControl    = options.CacheControl,
                ContentEncoding = options.ContentEncoding,
            };
            if (options.Metadata is { } meta)
                uploadOptions.Metadata = meta.ToDictionary(k => k.Key, v => v.Value);
        }

        await blob.UploadAsync(content, uploadOptions, ct);
        _logger.LogDebug("Uploaded {ObjectName} to Azure container {Bucket}", objectName, bucket);
    }

    // ── Download ─────────────────────────────────────────────────────────────

    public async Task<Stream> DownloadAsync(string bucket, string objectName,
        DownloadOptions? options = null, CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(bucket).GetBlobClient(objectName);

        BlobDownloadOptions? downloadOptions = null;
        if (options?.FromByte is { } from)
        {
            downloadOptions = new BlobDownloadOptions
            {
                Range = options.ToByte is { } to
                    ? new HttpRange(from, to - from + 1)
                    : new HttpRange(from),
            };
        }

        var response = await blob.DownloadStreamingAsync(downloadOptions, ct);
        return response.Value.Content;
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    public async Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(bucket).GetBlobClient(objectName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    // ── Exists ───────────────────────────────────────────────────────────────

    public async Task<bool> ExistsAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var blob     = _client.GetBlobContainerClient(bucket).GetBlobClient(objectName);
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }

    // ── GetMetadata ──────────────────────────────────────────────────────────

    public async Task<StorageObject?> GetMetadataAsync(string bucket, string objectName,
        CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(bucket).GetBlobClient(objectName);
        try
        {
            var props = await blob.GetPropertiesAsync(cancellationToken: ct);
            return new StorageObject
            {
                Name         = objectName,
                SizeBytes    = props.Value.ContentLength,
                ContentType  = props.Value.ContentType,
                ETag         = props.Value.ETag.ToString(),
                LastModified = props.Value.LastModified,
                Metadata     = props.Value.Metadata.AsReadOnly(),
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    // ── List ─────────────────────────────────────────────────────────────────

    public async Task<StorageObjectList> ListAsync(string bucket, ListOptions? options = null,
        CancellationToken ct = default)
    {
        var container  = _client.GetBlobContainerClient(bucket);
        var prefix     = options?.Prefix;
        var maxResults = options?.MaxResults ?? 100;
        var items      = new List<StorageObject>(maxResults);

        await foreach (var blob in container.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
        {
            if (items.Count >= maxResults)
                break;

            items.Add(new StorageObject
            {
                Name         = blob.Name,
                SizeBytes    = blob.Properties.ContentLength ?? 0,
                ContentType  = blob.Properties.ContentType,
                ETag         = blob.Properties.ETag?.ToString(),
                LastModified = blob.Properties.LastModified,
                Metadata     = blob.Metadata?.AsReadOnly()
                               ?? (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(),
            });
        }

        return new StorageObjectList { Items = items };
    }

    // ── Signed URL ───────────────────────────────────────────────────────────

    public Task<Uri> GetSignedUrlAsync(string bucket, string objectName,
        SignedUrlOptions options, CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(bucket).GetBlobClient(objectName);

        if (!blob.CanGenerateSasUri)
            throw new NotSupportedException(
                "Cannot generate a SAS URI: the BlobServiceClient was not created with a " +
                "StorageSharedKeyCredential. Use a connection string or account key, or " +
                "register the BlobServiceClient directly with the appropriate credential.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = bucket,
            BlobName          = objectName,
            Resource          = "b",
            ExpiresOn         = DateTimeOffset.UtcNow.Add(options.Expiry),
        };

        var permissions = options.Verb switch
        {
            SignedUrlVerb.Get    => BlobSasPermissions.Read,
            SignedUrlVerb.Put    => BlobSasPermissions.Write | BlobSasPermissions.Create,
            SignedUrlVerb.Delete => BlobSasPermissions.Delete,
            _                   => BlobSasPermissions.Read,
        };
        sasBuilder.SetPermissions(permissions);

        return Task.FromResult(blob.GenerateSasUri(sasBuilder));
    }
}
