namespace Primitives.Authentication.Abstractions;

/// <summary>
/// Resolves a registered <see cref="IAuthenticationStrategy"/> by name.
/// </summary>
public interface IAuthenticationStrategyFactory
{
    /// <summary>Returns the strategy registered under <paramref name="strategyName"/>.</summary>
    /// <exception cref="KeyNotFoundException">Thrown when no strategy with that name is registered.</exception>
    IAuthenticationStrategy GetStrategy(string strategyName);

    /// <summary>
    /// Attempts to resolve a strategy without throwing.
    /// Returns <see langword="false"/> when the strategy is not registered.
    /// </summary>
    bool TryGetStrategy(string strategyName, out IAuthenticationStrategy? strategy);

    /// <summary>Returns the names of all registered strategies.</summary>
    IReadOnlyCollection<string> RegisteredStrategyNames { get; }
}
