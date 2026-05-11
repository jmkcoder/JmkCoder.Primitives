namespace Primitives.Storage.Models;

/// <summary>Controls how an object is downloaded (byte-range support).</summary>
public sealed class DownloadOptions
{
    /// <summary>First byte offset to include (0-based, inclusive). <see langword="null"/> starts from the beginning.</summary>
    public long? FromByte { get; set; }

    /// <summary>Last byte offset to include (0-based, inclusive). <see langword="null"/> reads to end of object.</summary>
    public long? ToByte { get; set; }
}
