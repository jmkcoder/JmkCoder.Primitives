using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Primitives.Storage.Abstractions;
using Primitives.Storage.Azure.Internal;

namespace Primitives.Storage.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IStorageService"/> using Azure Blob Storage.
    /// Creates a <see cref="BlobServiceClient"/> from <see cref="AzureBlobStorageOptions"/>
    /// (connection string or account name + key).
    /// </summary>
    /// <remarks>
    /// For managed identity or other token credentials, use the overload that accepts a
    /// pre-built <see cref="BlobServiceClient"/>.
    /// </remarks>
    public static IServiceCollection AddPrimitivesStorageAzure(
        this IServiceCollection services,
        Action<AzureBlobStorageOptions> configureAzure,
        Action<StorageOptions>? configureStorage = null)
    {
        services.AddLogging();
        services.Configure<StorageOptions>(configureStorage ?? (_ => { }));
        services.Configure<AzureBlobStorageOptions>(configureAzure);

        services.TryAddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
            return CreateBlobServiceClient(opts);
        });

        services.TryAddSingleton<IStorageService, AzureBlobStorageService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IStorageService"/> using an existing <see cref="BlobServiceClient"/>.
    /// Use this overload for managed identity, DefaultAzureCredential, or custom configurations.
    /// </summary>
    public static IServiceCollection AddPrimitivesStorageAzure(
        this IServiceCollection services,
        BlobServiceClient client,
        Action<AzureBlobStorageOptions>? configureAzure = null,
        Action<StorageOptions>? configureStorage = null)
    {
        services.AddLogging();
        services.Configure<StorageOptions>(configureStorage ?? (_ => { }));
        services.Configure<AzureBlobStorageOptions>(configureAzure ?? (_ => { }));
        services.TryAddSingleton(client);
        services.TryAddSingleton<IStorageService, AzureBlobStorageService>();
        return services;
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    private static BlobServiceClient CreateBlobServiceClient(AzureBlobStorageOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
            return new BlobServiceClient(opts.ConnectionString);

        if (!string.IsNullOrWhiteSpace(opts.AccountName) && !string.IsNullOrWhiteSpace(opts.AccountKey))
        {
            var credential = new StorageSharedKeyCredential(opts.AccountName, opts.AccountKey);
            var uri        = new Uri($"https://{opts.AccountName}.blob.core.windows.net");
            return new BlobServiceClient(uri, credential);
        }

        throw new InvalidOperationException(
            "AzureBlobStorageOptions requires either ConnectionString or both AccountName and " +
            "AccountKey. For managed identity use the AddPrimitivesStorageAzure(BlobServiceClient, …) " +
            "overload with a client created from DefaultAzureCredential.");
    }
}
