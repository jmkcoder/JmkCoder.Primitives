namespace Primitives.Authentication.Resilience;

/// <summary>
/// Configuration for the authentication resilience pipeline.
/// Applied to <see cref="Strategies.TokenIssuance.ITokenIssuanceService"/> calls.
/// </summary>
public sealed class AuthenticationResilienceOptions
{
    /// <summary>
    /// Maximum number of retry attempts for transient authentication failures.
    /// Retries use exponential back-off with jitter.
    /// Defaults to <c>3</c>.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts.
    /// Actual delay is randomised: <c>base * (2 ^ attempt)</c> ± jitter.
    /// Defaults to <c>500ms</c>.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Duration the circuit breaker stays open after tripping.
    /// Defaults to <c>30 seconds</c>.
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of consecutive failures that trip the circuit breaker.
    /// Defaults to <c>5</c>.
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Overall timeout per <c>AuthenticateAsync</c> / <c>RefreshAsync</c> call.
    /// Defaults to <c>30 seconds</c>. Set to <see cref="Timeout.InfiniteTimeSpan"/> to disable.
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
