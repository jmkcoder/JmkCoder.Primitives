using Microsoft.Extensions.Diagnostics.HealthChecks;
using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.HealthChecks;

/// <summary>
/// Reports the health of all registered <see cref="IAuthenticationStrategy"/> instances
/// by calling <see cref="IAuthenticationStrategy.CanHandleAsync"/> on each one.
///
/// Health states:
/// <list type="table">
///   <item><term>Healthy</term><description>All strategies report they can handle requests.</description></item>
///   <item><term>Degraded</term><description>Some (but not all) strategies are ready.</description></item>
///   <item><term>Unhealthy</term><description>No strategy is ready, or an exception was thrown.</description></item>
/// </list>
/// </summary>
public sealed class AuthenticationHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IAuthenticationStrategy> _strategies;

    public AuthenticationHealthCheck(IEnumerable<IAuthenticationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(string Name, bool Ready)>();
        Exception? lastException = null;

        foreach (var strategy in _strategies)
        {
            try
            {
                var ready = await strategy.CanHandleAsync(cancellationToken);
                results.Add((strategy.Name, ready));
            }
            catch (Exception ex)
            {
                lastException = ex;
                results.Add((strategy.Name, false));
            }
        }

        if (results.Count == 0)
            return HealthCheckResult.Unhealthy("No authentication strategies are registered.");

        var readyCount = results.Count(r => r.Ready);
        var data = results.ToDictionary(
            r => r.Name,
            r => (object)(r.Ready ? "ready" : "not-ready"));

        if (readyCount == results.Count)
            return HealthCheckResult.Healthy("All authentication strategies are ready.", data);

        if (readyCount == 0)
        {
            return HealthCheckResult.Unhealthy(
                "No authentication strategies are ready.",
                lastException, data);
        }

        return HealthCheckResult.Degraded(
            $"{readyCount}/{results.Count} authentication strategies are ready.",
            lastException, data);
    }
}
