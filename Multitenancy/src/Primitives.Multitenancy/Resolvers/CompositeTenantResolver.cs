using Microsoft.AspNetCore.Http;
using Primitives.Multitenancy.Abstractions;

namespace Primitives.Multitenancy.Resolvers;

/// <summary>
/// Tries each registered <see cref="ITenantResolverStrategy"/> in registration order,
/// returning the first non-null result. Used as the default <see cref="ITenantResolver"/>
/// when strategies are registered via the fluent <c>.WithXxxResolver()</c> builder methods.
/// </summary>
internal sealed class CompositeTenantResolver : ITenantResolver
{
    private readonly IEnumerable<ITenantResolverStrategy> _strategies;

    public CompositeTenantResolver(IEnumerable<ITenantResolverStrategy> strategies)
        => _strategies = strategies;

    public async Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        foreach (var strategy in _strategies)
        {
            var result = await strategy.ResolveAsync(context, ct);
            if (result is not null)
                return result;
        }
        return null;
    }
}
