namespace Primitives.Storage.Models;

/// <summary>Controls how an object is stored.</summary>
public sealed class UploadOptions
{
    /// <summary>MIME type of the content (e.g. <c>"image/jpeg"</c>).</summary>
    public string? ContentType { get; set; }

    /// <summary>User-defined metadata to attach to the object.</summary>
    public IDictionary<string, string>? Metadata { get; set; }

    /// <summary>HTTP Cache-Control header value to associate with the object.</summary>
    public string? CacheControl { get; set; }

    /// <summary>HTTP Content-Encoding header value to associate with the object.</summary>
    public string? ContentEncoding { get; set; }
}
