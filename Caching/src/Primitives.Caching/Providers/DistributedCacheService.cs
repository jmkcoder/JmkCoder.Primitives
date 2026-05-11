using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Caching.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Primitives.Caching.Providers;

/// <summary>
/// <see cref="ICacheService"/> backed by <see cref="IDistributedCache"/>.
/// Values are JSON-serialised before storage.
/// Tag tracking is in-process only — if multiple app instances are running,
/// consider using <c>Primitives.Caching.Redis</c> for cross-node invalidation.
/// </summary>
internal sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagIndex = new();

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DistributedCacheService(
        IDistributedCache cache,
        IOptions<CacheOptions> options,
        ILogger<DistributedCacheService> logger)
    {
        _cache   = cache;
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        T? value;
        try
        {
            value = await factory(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!_options.PropagateFactoryExceptions)
        {
            _logger.LogWarning(ex, "Cache factory for key {Key} threw; returning default.", key);
            return default;
        }

        await SetAsync(key, value, options, cancellationToken).ConfigureAwait(false);
        return value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(PrefixedKey(key), cancellationToken).ConfigureAwait(false);
        if (bytes is null or { Length: 0 })
            return default;

        return JsonSerializer.Deserialize<T>(bytes, _json);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prefixedKey  = PrefixedKey(key);
        var bytes        = JsonSerializer.SerializeToUtf8Bytes(value, _json);
        var entryOptions = BuildDistributedCacheEntryOptions(options);

        await _cache.SetAsync(prefixedKey, bytes, entryOptions, cancellationToken).ConfigureAwait(false);

        if (options?.Tags is { Count: > 0 } tags)
        {
            foreach (var tag in tags)
            {
                _tagIndex
                    .GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>())
                    .TryAdd(prefixedKey, 0);
            }
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default) =>
        await _cache.RemoveAsync(PrefixedKey(key), cancellationToken).ConfigureAwait(false);

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (_tagIndex.TryRemove(tag, out var keys))
        {
            var tasks = keys.Keys.Select(k => _cache.RemoveAsync(k, cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string PrefixedKey(string key) =>
        string.IsNullOrEmpty(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}:{key}";

    private DistributedCacheEntryOptions BuildDistributedCacheEntryOptions(CacheEntryOptions? options)
    {
        var dco = new DistributedCacheEntryOptions();

        if (options?.AbsoluteExpiration is { } abs)
            dco.AbsoluteExpirationRelativeToNow = abs;
        else if (options?.SlidingExpiration is { } slide)
            dco.SlidingExpiration = slide;
        else
            dco.AbsoluteExpirationRelativeToNow = _options.DefaultAbsoluteExpiration;

        return dco;
    }
}
