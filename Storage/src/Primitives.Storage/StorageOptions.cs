namespace Primitives.Storage;

/// <summary>Top-level configuration options for <c>Primitives.Storage</c>.</summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Root directory used by the local filesystem provider.
    /// Defaults to the system temporary directory.
    /// Ignored by cloud providers.
    /// </summary>
    public string BasePath { get; set; } = Path.GetTempPath();

    /// <summary>
    /// Maximum allowed upload size in bytes. Defaults to 100 MB (104,857,600 bytes).
    /// The local provider enforces this limit; cloud providers may enforce their own limits
    /// independently.
    /// </summary>
    public long MaxUploadSizeBytes { get; set; } = 100L * 1024 * 1024;
}
