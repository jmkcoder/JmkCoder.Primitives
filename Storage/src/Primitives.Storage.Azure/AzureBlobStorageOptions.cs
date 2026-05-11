namespace Primitives.Storage.Azure;

/// <summary>Configuration for the Azure Blob Storage provider.</summary>
public sealed class AzureBlobStorageOptions
{
    /// <summary>
    /// Azure Storage connection string (e.g. <c>DefaultEndpointsProtocol=https;AccountName=…</c>).
    /// Takes precedence over <see cref="AccountName"/> + <see cref="AccountKey"/> if both are set.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>Storage account name. Used with <see cref="AccountKey"/> when no connection string is provided.</summary>
    public string? AccountName { get; set; }

    /// <summary>Storage account key. Used with <see cref="AccountName"/> when no connection string is provided.</summary>
    public string? AccountKey { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default), automatically creates a blob container if it does not
    /// exist before the first write operation. Disable in production if containers are pre-provisioned.
    /// </summary>
    public bool CreateContainersIfNotExist { get; set; } = true;
}
