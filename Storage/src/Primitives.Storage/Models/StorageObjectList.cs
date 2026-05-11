namespace Primitives.Storage.Models;

/// <summary>Result of a <see cref="Abstractions.IStorageService.ListAsync"/> call.</summary>
public sealed class StorageObjectList
{
    /// <summary>Objects returned for this page.</summary>
    public IReadOnlyList<StorageObject> Items { get; init; } = Array.Empty<StorageObject>();

    /// <summary>
    /// Opaque token to pass as <see cref="ListOptions.ContinuationToken"/> to retrieve the next page.
    /// <see langword="null"/> when there are no further results.
    /// </summary>
    public string? ContinuationToken { get; init; }
}
