using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Polly;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Caching;
using Primitives.Authentication.HealthChecks;
using Primitives.Authentication.Resilience;
using Primitives.Authentication.Strategies.ApiKey;
using Primitives.Authentication.Strategies.Kerberos;
using Primitives.Authentication.Strategies.Oidc;
using Primitives.Authentication.Strategies.TokenIssuance;
using Primitives.Authentication.Strategies.UsernamePassword;

namespace Primitives.Authentication.Extensions;

/// <summary>
/// Fluent builder returned by <see cref="ServiceCollectionExtensions.AddAuthentication(IServiceCollection)"/>.
/// Use the <c>Add*</c> extension methods to register one or more strategies.
/// </summary>
public sealed class AuthenticationBuilder
{
    internal AuthenticationBuilder(IServiceCollection services) => Services = services;

    /// <summary>Underlying service collection -- use this for advanced registrations.</summary>
    public IServiceCollection Services { get; }

    // -- OIDC ---------------------------------------------------------------

    /// <summary>
    /// Registers an <see cref="OidcAuthenticationStrategy"/> with the default name <c>"OIDC"</c>.
    /// </summary>
    public AuthenticationBuilder AddOidc(Action<OidcAuthenticationOptions> configure)
        => AddOidc("OIDC", configure);

    /// <summary>
    /// Registers an <see cref="OidcAuthenticationStrategy"/> with a custom <paramref name="name"/>.
    /// Use this overload to register multiple OIDC strategies (e.g. two different tenants).
    /// </summary>
    public AuthenticationBuilder AddOidc(string name, Action<OidcAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.AddOptions<OidcAuthenticationOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        Services.AddTransient<IAuthenticationStrategy>(sp => new OidcAuthenticationStrategy(
            name,
            sp.GetRequiredService<IOptionsMonitor<OidcAuthenticationOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OidcAuthenticationStrategy>>()));
        return this;
    }

    // -- Username / Password ------------------------------------------------

    /// <summary>
    /// Registers a <see cref="UsernamePasswordAuthenticationStrategy"/> with the default name
    /// <c>"UsernamePassword"</c>.
    /// </summary>
    public AuthenticationBuilder AddUsernamePassword(Action<UsernamePasswordAuthenticationOptions> configure)
        => AddUsernamePassword("UsernamePassword", configure);

    /// <summary>
    /// Registers a <see cref="UsernamePasswordAuthenticationStrategy"/> with a custom
    /// <paramref name="name"/>.
    /// </summary>
    public AuthenticationBuilder AddUsernamePassword(
        string name,
        Action<UsernamePasswordAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.AddOptions<UsernamePasswordAuthenticationOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        Services.AddTransient<IAuthenticationStrategy>(sp => new UsernamePasswordAuthenticationStrategy(
            name,
            sp.GetRequiredService<IOptionsMonitor<UsernamePasswordAuthenticationOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UsernamePasswordAuthenticationStrategy>>()));
        return this;
    }

    // -- Kerberos -----------------------------------------------------------

    /// <summary>
    /// Registers a <see cref="KerberosAuthenticationStrategy"/> with the default name
    /// <c>"Kerberos"</c>.
    /// </summary>
    public AuthenticationBuilder AddKerberos(Action<KerberosAuthenticationOptions> configure)
        => AddKerberos("Kerberos", configure);

    /// <summary>
    /// Registers a <see cref="KerberosAuthenticationStrategy"/> with a custom <paramref name="name"/>.
    /// </summary>
    public AuthenticationBuilder AddKerberos(string name, Action<KerberosAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.AddOptions<KerberosAuthenticationOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        Services.AddTransient<IAuthenticationStrategy>(sp => new KerberosAuthenticationStrategy(
            name,
            sp.GetRequiredService<IOptionsMonitor<KerberosAuthenticationOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KerberosAuthenticationStrategy>>()));
        return this;
    }

    // -- API Key ------------------------------------------------------------

    /// <summary>
    /// Registers an <see cref="ApiKeyAuthenticationStrategy"/> with the default name
    /// <c>"ApiKey"</c>.
    /// </summary>
    public AuthenticationBuilder AddApiKey(Action<ApiKeyAuthenticationOptions> configure)
        => AddApiKey("ApiKey", configure);

    /// <summary>
    /// Registers an <see cref="ApiKeyAuthenticationStrategy"/> with a custom <paramref name="name"/>.
    /// </summary>
    public AuthenticationBuilder AddApiKey(string name, Action<ApiKeyAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.AddOptions<ApiKeyAuthenticationOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        Services.AddTransient<IAuthenticationStrategy>(sp => new ApiKeyAuthenticationStrategy(
            name,
            sp.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiKeyAuthenticationStrategy>>()));
        return this;
    }

    // -- Custom -------------------------------------------------------------

    /// <summary>
    /// Registers a custom strategy implementation as <see cref="IAuthenticationStrategy"/>.
    /// </summary>
    public AuthenticationBuilder AddCustomStrategy<TStrategy>()
        where TStrategy : class, IAuthenticationStrategy
    {
        Services.AddTransient<IAuthenticationStrategy, TStrategy>();
        return this;
    }

    // -- JWT token issuance -------------------------------------------------

    /// <summary>
    /// Registers JWT generation, rolling refresh token services, and an
    /// <see cref="IJwtTokenValidator"/> for validating inbound tokens.
    /// </summary>
    public AuthenticationBuilder AddJwtTokenIssuance(Action<JwtOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.AddOptions<JwtOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        Services.TryAddSingleton<IJwtTokenService, JwtTokenService>();
        Services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        Services.TryAddSingleton<IJwtTokenValidator, JwtTokenValidator>();
        Services.TryAddTransient<ITokenIssuanceService, TokenIssuanceService>();
        return this;
    }

    // -- Result cache (in-memory) -------------------------------------------

    /// <summary>
    /// Enables in-process caching of successful authentication results to avoid
    /// redundant strategy calls for the same identity within a token's lifetime.
    /// </summary>
    public AuthenticationBuilder AddResultCache(Action<AuthenticationCacheOptions>? configure = null)
    {
        Services.AddMemoryCache();
        Services.AddOptions<AuthenticationCacheOptions>();
        if (configure is not null)
            Services.Configure(configure);

        Services.TryAddSingleton<IAuthenticationResultCache, InMemoryAuthenticationResultCache>();
        return this;
    }

    // -- Result cache (distributed) -----------------------------------------

    /// <summary>
    /// Enables distributed caching of successful authentication results using
    /// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>.
    /// Replaces any previously registered <see cref="IAuthenticationResultCache"/>.
    /// </summary>
    /// <remarks>
    /// Register a concrete distributed cache provider before calling this method, e.g.:
    /// <code>
    /// services.AddStackExchangeRedisCache(o => o.Configuration = "localhost");
    /// services.AddAuthentication().AddDistributedResultCache();
    /// </code>
    /// </remarks>
    public AuthenticationBuilder AddDistributedResultCache(
        Action<AuthenticationCacheOptions>? configure = null)
    {
        Services.AddOptions<AuthenticationCacheOptions>();
        if (configure is not null)
            Services.Configure(configure);

        Services.Replace(ServiceDescriptor.Singleton<IAuthenticationResultCache,
                                                     DistributedAuthenticationResultCache>());
        return this;
    }

    // -- Distributed refresh token store ------------------------------------

    /// <summary>
    /// Replaces the in-memory <see cref="IRefreshTokenStore"/> with a distributed
    /// implementation backed by <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>.
    /// Call this <em>after</em> <see cref="AddJwtTokenIssuance"/>.
    /// </summary>
    /// <remarks>
    /// Register a concrete distributed cache provider before calling this method.
    ///
    /// <strong>Limitation:</strong> Successor-chain revocation is not supported in the distributed
    /// variant. See <see cref="DistributedRefreshTokenStore"/> for details.
    /// </remarks>
    public AuthenticationBuilder AddDistributedRefreshTokenStore()
    {
        Services.Replace(ServiceDescriptor.Singleton<IRefreshTokenStore,
                                                     DistributedRefreshTokenStore>());
        return this;
    }

    // -- Health check -------------------------------------------------------

    /// <summary>
    /// Registers an <see cref="AuthenticationHealthCheck"/> with the standard .NET
    /// health-checks infrastructure.
    /// Use <c>services.AddHealthChecks()</c> separately in your application startup.
    /// </summary>
    /// <param name="name">Health-check registration name. Defaults to <c>"authentication"</c>.</param>
    /// <param name="failureStatus">
    /// Status to report when one or more strategies are not ready.
    /// Defaults to <see cref="HealthStatus.Degraded"/>.
    /// </param>
    /// <param name="tags">Optional tags applied to this health check.</param>
    public AuthenticationBuilder AddHealthCheck(
        string name                 = "authentication",
        HealthStatus? failureStatus = HealthStatus.Degraded,
        IEnumerable<string>? tags   = null)
    {
        Services.AddHealthChecks()
                .AddCheck<AuthenticationHealthCheck>(name, failureStatus, tags ?? []);
        return this;
    }

    // -- Resilience ---------------------------------------------------------

    /// <summary>
    /// Adds a resilience pipeline (retry + circuit-breaker + timeout) around
    /// <see cref="ITokenIssuanceService"/> calls using Microsoft.Extensions.Resilience.
    /// </summary>
    public AuthenticationBuilder AddAuthenticationResilience(
        Action<AuthenticationResilienceOptions>? configure = null)
    {
        var opts = new AuthenticationResilienceOptions();
        configure?.Invoke(opts);

        Services.AddResiliencePipeline<string, AuthenticationResult>(
            "Primitives.Authentication",
            builder =>
            {
                builder
                    .AddTimeout(opts.OperationTimeout)
                    .AddRetry(new Polly.Retry.RetryStrategyOptions<AuthenticationResult>
                    {
                        MaxRetryAttempts  = opts.MaxRetries,
                        Delay             = opts.RetryBaseDelay,
                        BackoffType       = DelayBackoffType.Exponential,
                        UseJitter         = true,
                        ShouldHandle      = args =>
                            ValueTask.FromResult(!args.Outcome.Result?.IsSuccess ?? args.Outcome.Exception is not null),
                    })
                    .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<AuthenticationResult>
                    {
                        FailureRatio             = 0.5,
                        MinimumThroughput        = opts.CircuitBreakerThreshold,
                        SamplingDuration         = TimeSpan.FromSeconds(30),
                        BreakDuration            = opts.CircuitBreakerDuration,
                        ShouldHandle             = args =>
                            ValueTask.FromResult(!args.Outcome.Result?.IsSuccess ?? args.Outcome.Exception is not null),
                    });
            });

        return this;
    }
}