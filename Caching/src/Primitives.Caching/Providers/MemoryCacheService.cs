using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Caching.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Primitives.Caching.Providers;

/// <summary>
/// <see cref="ICacheService"/> backed by <see cref="IMemoryCache"/>.
/// Tag-to-key tracking is kept in a <see cref="ConcurrentDictionary"/> in process memory.
/// </summary>
internal sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<MemoryCacheService> _logger;

    // tag → set of keys that carry that tag
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagIndex = new();

    public MemoryCacheService(
        IMemoryCache cache,
        IOptions<CacheOptions> options,
        ILogger<MemoryCacheService> logger)
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
        var prefixedKey = PrefixedKey(key);

        if (_cache.TryGetValue(prefixedKey, out T? cached))
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

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(PrefixedKey(key), out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prefixedKey = PrefixedKey(key);
        var entryOptions = BuildMemoryCacheEntryOptions(options);

        _cache.Set(prefixedKey, value, entryOptions);

        if (options?.Tags is { Count: > 0 } tags)
        {
            foreach (var tag in tags)
            {
                _tagIndex
                    .GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>())
                    .TryAdd(prefixedKey, 0);
            }
        }

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(PrefixedKey(key));
        return Task.CompletedTask;
    }

    public Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (_tagIndex.TryRemove(tag, out var keys))
        {
            foreach (var k in keys.Keys)
                _cache.Remove(k);
        }

        return Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string PrefixedKey(string key) =>
        string.IsNullOrEmpty(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}:{key}";

    private MemoryCacheEntryOptions BuildMemoryCacheEntryOptions(CacheEntryOptions? options)
    {
        var mco = new MemoryCacheEntryOptions();

        if (options?.AbsoluteExpiration is { } abs)
            mco.AbsoluteExpirationRelativeToNow = abs;
        else if (options?.SlidingExpiration is { } slide)
            mco.SlidingExpiration = slide;
        else
            mco.AbsoluteExpirationRelativeToNow = _options.DefaultAbsoluteExpiration;

        return mco;
    }
}
