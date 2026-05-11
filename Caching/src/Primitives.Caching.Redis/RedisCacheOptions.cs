namespace Primitives.Caching.Redis;

/// <summary>Configuration options for the Redis cache provider.</summary>
public sealed class RedisCacheOptions
{
    /// <summary>
    /// StackExchange.Redis configuration string, e.g.
    /// <c>"localhost:6379"</c> or <c>"redis.prod.internal:6379,password=secret"</c>.
    /// Takes precedence over <see cref="ConnectionMultiplexerFactory"/> when set.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Optional factory that creates or returns an existing
    /// <c>IConnectionMultiplexer</c>. Use this when you want to share a
    /// single multiplexer across your application.
    /// </summary>
    public Func<IServiceProvider, StackExchange.Redis.IConnectionMultiplexer>? ConnectionMultiplexerFactory { get; set; }

    /// <summary>
    /// Redis database index. Defaults to <c>-1</c> (the default database configured
    /// in the connection string, normally DB 0).
    /// </summary>
    public int DatabaseIndex { get; set; } = -1;

    /// <summary>
    /// When <c>true</c> (default), tag-based invalidation uses Redis pub/sub so
    /// all instances that subscribe to the same channel are notified and purge
    /// their local in-process tag index. Set to <c>false</c> to use local-only
    /// tag tracking (single-node only).
    /// </summary>
    public bool UsePubSubInvalidation { get; set; } = true;

    /// <summary>
    /// Pub/sub channel name used for cross-node tag invalidation messages.
    /// Defaults to <c>"primitives:caching:invalidation"</c>.
    /// </summary>
    public string InvalidationChannel { get; set; } = "primitives:caching:invalidation";
}
