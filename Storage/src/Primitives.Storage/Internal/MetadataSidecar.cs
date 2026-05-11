using System.Text.Json.Serialization;

namespace Primitives.Storage.Internal;

/// <summary>JSON sidecar stored alongside local filesystem objects to preserve upload metadata.</summary>
internal sealed class MetadataSidecar
{
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("cacheControl")]
    public string? CacheControl { get; set; }

    [JsonPropertyName("contentEncoding")]
    public string? ContentEncoding { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
