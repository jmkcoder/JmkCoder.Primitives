using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Exceptions;

namespace Primitives.Authentication.Factory;

/// <summary>
/// Resolves named <see cref="IAuthenticationStrategy"/> instances from the registered set.
/// All strategies are injected via <see cref="IEnumerable{T}"/> so they integrate naturally
/// with the Microsoft DI container — simply register each concrete strategy as
/// <c>IAuthenticationStrategy</c> and the factory discovers them automatically.
/// </summary>
public sealed class AuthenticationStrategyFactory : IAuthenticationStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IAuthenticationStrategy> _strategies;

    public AuthenticationStrategyFactory(IEnumerable<IAuthenticationStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            s => s.Name,
            s => s,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> RegisteredStrategyNames =>
        (IReadOnlyCollection<string>)_strategies.Keys;

    /// <inheritdoc/>
    public IAuthenticationStrategy GetStrategy(string strategyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);
        return _strategies.TryGetValue(strategyName, out var strategy)
            ? strategy
            : throw new AuthenticationException(
                strategyName,
                $"No authentication strategy named '{strategyName}' is registered. " +
                $"Available: {string.Join(", ", _strategies.Keys)}",
                AuthenticationFailureReason.StrategyNotFound);
    }

    /// <inheritdoc/>
    public bool TryGetStrategy(string strategyName, out IAuthenticationStrategy? strategy)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            strategy = null;
            return false;
        }

        return _strategies.TryGetValue(strategyName, out strategy);
    }
}
