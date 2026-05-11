using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Primitives.Storage.Models;

namespace Primitives.Storage.Abstractions;

/// <summary>
/// Abstraction over a blob/object storage backend. All operations are bucket-scoped.
/// The <paramref name="bucket"/> maps to a container (Azure), bucket (S3), or directory (local).
/// The <paramref name="objectName"/> may contain forward slashes to represent a virtual folder hierarchy.
/// </summary>
/// <remarks>
/// <see cref="DownloadAsync"/> returns an owned <see cref="Stream"/> — callers are responsible for disposing it.
/// </remarks>
public interface IStorageService
{
    /// <summary>Uploads <paramref name="content"/> to the specified object, replacing any existing object.</summary>
    Task UploadAsync(string bucket, string objectName, Stream content,
        UploadOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Opens a stream to the object content. The caller owns and must dispose the returned stream.
    /// </summary>
    /// <exception cref="Exceptions.StorageException">The object does not exist.</exception>
    Task<Stream> DownloadAsync(string bucket, string objectName,
        DownloadOptions? options = null, CancellationToken ct = default);

    /// <summary>Deletes the object. No-ops silently if the object does not exist.</summary>
    Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the object exists; otherwise <see langword="false"/>.</summary>
    Task<bool> ExistsAsync(string bucket, string objectName, CancellationToken ct = default);

    /// <summary>Returns metadata for the object, or <see langword="null"/> if it does not exist.</summary>
    Task<StorageObject?> GetMetadataAsync(string bucket, string objectName, CancellationToken ct = default);

    /// <summary>Lists objects in <paramref name="bucket"/>, optionally filtered by prefix.</summary>
    Task<StorageObjectList> ListAsync(string bucket, ListOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Generates a pre-signed URL granting temporary access to the object.
    /// </summary>
    /// <exception cref="NotSupportedException">The provider does not support signed URLs.</exception>
    Task<Uri> GetSignedUrlAsync(string bucket, string objectName,
        SignedUrlOptions options, CancellationToken ct = default);
}
