namespace Primitives.Storage.Models;

/// <summary>Options for listing objects in a bucket.</summary>
public sealed class ListOptions
{
    /// <summary>
    /// Only return objects whose names start with this prefix.
    /// <see langword="null"/> returns all objects.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>Maximum number of objects to return per page. Defaults to 100.</summary>
    public int MaxResults { get; set; } = 100;

    /// <summary>Opaque continuation token from a previous <see cref="StorageObjectList.ContinuationToken"/>.</summary>
    public string? ContinuationToken { get; set; }
}
