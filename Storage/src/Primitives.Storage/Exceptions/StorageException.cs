namespace Primitives.Storage.Exceptions;

/// <summary>Thrown when a storage operation fails due to a domain-level error (e.g. object not found).</summary>
public sealed class StorageException : Exception
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception inner) : base(message, inner) { }
}
