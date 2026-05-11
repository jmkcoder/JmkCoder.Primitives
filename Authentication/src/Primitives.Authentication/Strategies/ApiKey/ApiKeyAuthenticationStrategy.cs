using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Diagnostics;

namespace Primitives.Authentication.Strategies.ApiKey;

/// <summary>
/// Implements API Key authentication.
/// Returns the configured key formatted according to the chosen <see cref="ApiKeyPlacement"/>.
/// </summary>
public sealed class ApiKeyAuthenticationStrategy : IAuthenticationStrategy
{
    private readonly string _name;
    private readonly IOptionsMonitor<ApiKeyAuthenticationOptions> _monitor;
    private readonly ILogger<ApiKeyAuthenticationStrategy> _logger;

    public string Name => _name;

    public ApiKeyAuthenticationStrategy(
        string name,
        IOptionsMonitor<ApiKeyAuthenticationOptions> monitor,
        ILogger<ApiKeyAuthenticationStrategy> logger)
    {
        _name    = name;
        _monitor = monitor;
        _logger  = logger;
    }

    public Task<bool> CanHandleAsync(CancellationToken cancellationToken = default)
    {
        var o = _monitor.Get(_name);
        return Task.FromResult(!string.IsNullOrWhiteSpace(o.ApiKey));
    }

    public Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var options = _monitor.Get(_name);

        using var activity = AuthenticationDiagnostics.Source.StartActivity(
            AuthenticationDiagnostics.ActivityStrategyExecute, ActivityKind.Internal);
        activity?.SetTag(AuthenticationDiagnostics.TagStrategyName, _name);

        try
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
                return Task.FromResult(AuthenticationResult.Failure("ApiKey must be configured."));
            }

            var (token, tokenType) = options.Placement switch
            {
                ApiKeyPlacement.BearerToken =>
                    (options.ApiKey, "Bearer"),

                ApiKeyPlacement.Header =>
                    (string.IsNullOrEmpty(options.HeaderPrefix)
                        ? options.ApiKey
                        : string.Concat(options.HeaderPrefix.TrimEnd(), options.ApiKey),
                     options.KeyName),

                ApiKeyPlacement.QueryParameter =>
                    (options.ApiKey, options.KeyName),

                _ => throw new NotSupportedException(
                    string.Format("Unsupported ApiKeyPlacement: {0}", options.Placement))
            };

            _logger.LogDebug("API Key credential resolved with placement '{Placement}' (strategy '{Name}')",
                options.Placement, _name);

            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, true);
            activity?.SetTag(AuthenticationDiagnostics.TagSubject,   options.KeyName);

            return Task.FromResult(AuthenticationResult.Success(token, tokenType, subject: options.KeyName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Key authentication failed for strategy '{Name}'", _name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return Task.FromResult(AuthenticationResult.Failure(ex.Message, ex));
        }
    }
}