using System.Diagnostics;
using System.Net;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Diagnostics;
using Primitives.Authentication.Exceptions;

namespace Primitives.Authentication.Strategies.Kerberos;

/// <summary>
/// Acquires a Kerberos / Negotiate service ticket using
/// <see cref="NegotiateAuthentication"/> (.NET 7+) and returns it as a
/// Base-64 encoded Negotiate token suitable for the HTTP Authorization header.
/// </summary>
public sealed class KerberosAuthenticationStrategy : IAuthenticationStrategy
{
    private readonly string _name;
    private readonly IOptionsMonitor<KerberosAuthenticationOptions> _monitor;
    private readonly ILogger<KerberosAuthenticationStrategy> _logger;

    public string Name => _name;

    public KerberosAuthenticationStrategy(
        string name,
        IOptionsMonitor<KerberosAuthenticationOptions> monitor,
        ILogger<KerberosAuthenticationStrategy> logger)
    {
        _name    = name;
        _monitor = monitor;
        _logger  = logger;
    }

    public Task<bool> CanHandleAsync(CancellationToken cancellationToken = default)
    {
        var o = _monitor.Get(_name);
        return Task.FromResult(!string.IsNullOrWhiteSpace(o.ServicePrincipalName));
    }

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var options = _monitor.Get(_name);

        if (string.IsNullOrWhiteSpace(options.ServicePrincipalName))
            return AuthenticationResult.Failure("ServicePrincipalName must be configured for Kerberos authentication.");

        using var activity = AuthenticationDiagnostics.Source.StartActivity(
            AuthenticationDiagnostics.ActivityStrategyExecute, ActivityKind.Client);
        activity?.SetTag(AuthenticationDiagnostics.TagStrategyName, _name);

        try
        {
            var clientOptions = BuildClientOptions(options);
            using var negotiateAuth = new NegotiateAuthentication(clientOptions);

            // Cast explicitly to ReadOnlySpan<byte> to resolve overload ambiguity between
            // GetOutgoingBlob(ReadOnlySpan<byte>, ...) and GetOutgoingBlob(string?, ...).
            var token = negotiateAuth.GetOutgoingBlob(
                incomingBlob: (ReadOnlySpan<byte>)default,
                out var statusCode);

            if (token is null || statusCode is not (NegotiateAuthenticationStatusCode.Completed
                                                  or NegotiateAuthenticationStatusCode.ContinueNeeded))
            {
                activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
                return AuthenticationResult.Failure(
                    string.Format("Kerberos token acquisition failed. Status: {0}", statusCode));
            }

            var base64Token = Convert.ToBase64String(token);
            _logger.LogDebug("Kerberos ticket acquired for SPN '{Spn}' using package '{Package}'",
                options.ServicePrincipalName, options.Package);

            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, true);
            activity?.SetTag(AuthenticationDiagnostics.TagSubject,   options.ServicePrincipalName);

            return AuthenticationResult.Success(base64Token, "Negotiate", subject: options.ServicePrincipalName);
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning(ex, "Kerberos is not supported on this platform");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            throw new AuthenticationException(_name, "Kerberos authentication is not supported on this platform.", ex,
                AuthenticationFailureReason.PlatformNotSupported);
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kerberos authentication failed for SPN '{Spn}'", options.ServicePrincipalName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return AuthenticationResult.Failure(ex.Message, ex);
        }
    }

    private static NegotiateAuthenticationClientOptions BuildClientOptions(KerberosAuthenticationOptions options)
    {
        var opts = new NegotiateAuthenticationClientOptions
        {
            Package    = options.Package,
            TargetName = options.ServicePrincipalName
        };

        if (options.Credential is not null)
        {
            opts.Credential = new NetworkCredential(
                options.Credential.UserName,
                options.Credential.Password,
                options.Credential.Domain);
        }

        return opts;
    }
}