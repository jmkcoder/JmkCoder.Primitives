using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Context;

/// <summary>
/// Holds the active <see cref="IAuthenticationStrategy"/> and delegates authentication calls to it.
/// This is the "Context" in the classic Strategy pattern — it allows the caller to swap strategies
/// at runtime without changing the calling code.
/// </summary>
public sealed class AuthenticationContext : IAuthenticationContext
{
    private IAuthenticationStrategy _strategy;
    private readonly object _lock = new();

    /// <param name="strategy">Initial strategy to use.</param>
    public AuthenticationContext(IAuthenticationStrategy strategy)
    {
        _strategy = strategy;
    }

    /// <inheritdoc/>
    public string ActiveStrategy
    {
        get
        {
            lock (_lock) return _strategy.Name;
        }
    }

    /// <inheritdoc/>
    public void SetStrategy(IAuthenticationStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        lock (_lock) _strategy = strategy;
    }

    /// <inheritdoc/>
    public Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        IAuthenticationStrategy current;
        lock (_lock) current = _strategy;
        return current.AuthenticateAsync(cancellationToken);
    }
}
