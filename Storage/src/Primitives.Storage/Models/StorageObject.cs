namespace Primitives.Storage.Models;

/// <summary>Metadata describing a stored object.</summary>
public sealed class StorageObject
{
    /// <summary>The object name (path) within its bucket.</summary>
    public required string Name { get; init; }

    /// <summary>Size of the object in bytes.</summary>
    public long SizeBytes { get; init; }

    /// <summary>MIME type of the object content, if known.</summary>
    public string? ContentType { get; init; }

    /// <summary>Entity tag, if provided by the storage backend.</summary>
    public string? ETag { get; init; }

    /// <summary>When the object was last modified, if provided by the storage backend.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>User-defined metadata key/value pairs.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
