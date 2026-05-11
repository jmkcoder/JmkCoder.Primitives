using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Diagnostics;

namespace Primitives.Authentication.Strategies.UsernamePassword;

/// <summary>
/// Implements HTTP Basic Authentication (RFC 7617).
/// Encodes "username:password" as Base-64 and returns it as an Authorization header value.
/// The encoded credentials are kept only in memory and are never written to disk.
/// </summary>
public sealed class UsernamePasswordAuthenticationStrategy : IAuthenticationStrategy
{
    private readonly string _name;
    private readonly IOptionsMonitor<UsernamePasswordAuthenticationOptions> _monitor;
    private readonly ILogger<UsernamePasswordAuthenticationStrategy> _logger;

    public string Name => _name;

    public UsernamePasswordAuthenticationStrategy(
        string name,
        IOptionsMonitor<UsernamePasswordAuthenticationOptions> monitor,
        ILogger<UsernamePasswordAuthenticationStrategy> logger)
    {
        _name    = name;
        _monitor = monitor;
        _logger  = logger;
    }

    public Task<bool> CanHandleAsync(CancellationToken cancellationToken = default)
    {
        var o = _monitor.Get(_name);
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(o.Username) &&
            !string.IsNullOrWhiteSpace(o.Password));
    }

    public Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var options = _monitor.Get(_name);

        using var activity = AuthenticationDiagnostics.Source.StartActivity(
            AuthenticationDiagnostics.ActivityStrategyExecute, ActivityKind.Internal);
        activity?.SetTag(AuthenticationDiagnostics.TagStrategyName, _name);

        try
        {
            var raw      = string.Concat(options.Username, ":", options.Password);
            var rawBytes = options.Encoding.GetBytes(raw);
            var encoded  = Convert.ToBase64String(rawBytes);

            // Wipe the intermediate plain-text bytes from the stack as soon as possible.
            Array.Clear(rawBytes, 0, rawBytes.Length);

            _logger.LogDebug("Basic Auth credentials encoded for user '{User}' (strategy '{Name}')",
                options.Username, _name);

            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, true);
            activity?.SetTag(AuthenticationDiagnostics.TagSubject,   options.Username);

            return Task.FromResult(AuthenticationResult.Success(encoded, "Basic", subject: options.Username));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encode Basic Auth credentials for user '{User}'", options.Username);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return Task.FromResult(AuthenticationResult.Failure(ex.Message, ex));
        }
    }
}