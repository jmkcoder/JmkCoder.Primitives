using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Caching.Abstractions;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Primitives.Caching.Redis;

/// <summary>
/// <see cref="ICacheService"/> backed by Redis (StackExchange.Redis).
/// Supports absolute and sliding expiry, tagged invalidation, and optional
/// pub/sub cross-node invalidation.
/// </summary>
internal sealed class RedisCacheService : ICacheService, IAsyncDisposable
{
    private readonly IDatabase _db;
    private readonly ISubscriber? _subscriber;
    private readonly RedisCacheOptions _redisOptions;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<RedisCacheService> _logger;

    // tag → set of Redis keys  (local index; authoritative copy lives as a Redis SET)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _localTagIndex = new();

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RedisCacheService(
        IConnectionMultiplexer multiplexer,
        IOptions<RedisCacheOptions> redisOptions,
        IOptions<CacheOptions> cacheOptions,
        ILogger<RedisCacheService> logger)
    {
        _redisOptions = redisOptions.Value;
        _cacheOptions = cacheOptions.Value;
        _logger       = logger;
        _db           = multiplexer.GetDatabase(_redisOptions.DatabaseIndex);

        if (_redisOptions.UsePubSubInvalidation)
        {
            _subscriber = multiplexer.GetSubscriber();
            _subscriber.Subscribe(
                RedisChannel.Literal(_redisOptions.InvalidationChannel),
                OnInvalidationMessage);
        }
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
        catch (Exception ex) when (!_cacheOptions.PropagateFactoryExceptions)
        {
            _logger.LogWarning(ex, "Cache factory for key {Key} threw; returning default.", key);
            return default;
        }

        await SetAsync(key, value, options, cancellationToken).ConfigureAwait(false);
        return value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(PrefixedKey(key)).ConfigureAwait(false);
        if (!value.HasValue)
            return default;

        return JsonSerializer.Deserialize<T>((string)value!, _json);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prefixedKey = PrefixedKey(key);
        var json        = JsonSerializer.Serialize(value, _json);
        var expiry      = ResolveExpiry(options);

        await _db.StringSetAsync(prefixedKey, json, expiry).ConfigureAwait(false);

        if (options?.Tags is { Count: > 0 } tags)
        {
            foreach (var tag in tags)
            {
                // Store key in a Redis SET named "tag:{tag}" so other nodes can read it
                var tagSetKey = TagSetKey(tag);
                await _db.SetAddAsync(tagSetKey, prefixedKey).ConfigureAwait(false);
                if (expiry.HasValue)
                    await _db.KeyExpireAsync(tagSetKey, expiry.Value * 2).ConfigureAwait(false);

                _localTagIndex
                    .GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>())
                    .TryAdd(prefixedKey, 0);
            }
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default) =>
        await _db.KeyDeleteAsync(PrefixedKey(key)).ConfigureAwait(false);

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var tagSetKey = TagSetKey(tag);
        var members   = await _db.SetMembersAsync(tagSetKey).ConfigureAwait(false);

        if (members.Length > 0)
        {
            var keys = members.Select(m => (RedisKey)(string)m!).ToArray();
            await _db.KeyDeleteAsync(keys).ConfigureAwait(false);
            await _db.KeyDeleteAsync(tagSetKey).ConfigureAwait(false);
        }

        // Publish to other nodes
        if (_redisOptions.UsePubSubInvalidation && _subscriber is not null)
        {
            await _subscriber.PublishAsync(
                RedisChannel.Literal(_redisOptions.InvalidationChannel),
                tag).ConfigureAwait(false);
        }

        _localTagIndex.TryRemove(tag, out _);
    }

    // ── Pub/sub handler (fires on the thread pool) ────────────────────────────

    private void OnInvalidationMessage(RedisChannel channel, RedisValue message)
    {
        var tag = (string?)message;
        if (tag is not null)
            _localTagIndex.TryRemove(tag, out _);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string PrefixedKey(string key) =>
        string.IsNullOrEmpty(_cacheOptions.KeyPrefix) ? key : $"{_cacheOptions.KeyPrefix}:{key}";

    private static string TagSetKey(string tag) => $"primitives:tag:{tag}";

    private TimeSpan? ResolveExpiry(CacheEntryOptions? options)
    {
        if (options?.AbsoluteExpiration is { } abs)  return abs;
        if (options?.SlidingExpiration  is { } slide) return slide;
        return _cacheOptions.DefaultAbsoluteExpiration;
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscriber is not null)
            await _subscriber.UnsubscribeAsync(
                RedisChannel.Literal(_redisOptions.InvalidationChannel)).ConfigureAwait(false);
    }
}
