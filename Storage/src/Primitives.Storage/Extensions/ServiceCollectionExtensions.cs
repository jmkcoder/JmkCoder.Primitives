using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Primitives.Storage.Abstractions;
using Primitives.Storage.Internal;

namespace Primitives.Storage.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IStorageService"/> using the local filesystem provider.
    /// Buckets map to sub-directories under <see cref="StorageOptions.BasePath"/>.
    /// </summary>
    public static IServiceCollection AddPrimitivesStorage(
        this IServiceCollection services,
        Action<StorageOptions>? configure = null)
    {
        services.AddLogging();
        services.Configure<StorageOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<IStorageService, LocalStorageService>();
        return services;
    }
}
