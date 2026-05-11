using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Diagnostics;
using Primitives.Authentication.Exceptions;
using MsalAuthResult = Microsoft.Identity.Client.AuthenticationResult;
using AuthenticationResult = Primitives.Authentication.Abstractions.AuthenticationResult;

namespace Primitives.Authentication.Strategies.Oidc;

/// <summary>
/// Authenticates via OIDC / OAuth 2.0 using MSAL.NET.
/// Supports Client Credentials (machine-to-machine) and ROPC (user + password) flows.
/// Token caching is handled transparently by the underlying MSAL <see cref="IClientApplicationBase"/>.
/// </summary>
public sealed class OidcAuthenticationStrategy : IAuthenticationStrategy
{
    private readonly string _name;
    private readonly IOptionsMonitor<OidcAuthenticationOptions> _monitor;
    private readonly ILogger<OidcAuthenticationStrategy> _logger;

    public string Name => _name;

    public OidcAuthenticationStrategy(
        string name,
        IOptionsMonitor<OidcAuthenticationOptions> monitor,
        ILogger<OidcAuthenticationStrategy> logger)
    {
        _name    = name;
        _monitor = monitor;
        _logger  = logger;
    }

    public Task<bool> CanHandleAsync(CancellationToken cancellationToken = default)
    {
        var o = _monitor.Get(_name);
        var ready = !string.IsNullOrWhiteSpace(o.Authority)
                 && !string.IsNullOrWhiteSpace(o.ClientId)
                 && !string.IsNullOrWhiteSpace(o.ClientSecret);

        if (o.Flow == OidcFlow.ResourceOwnerPassword)
            ready = ready
                 && !string.IsNullOrWhiteSpace(o.Username)
                 && !string.IsNullOrWhiteSpace(o.Password);

        return Task.FromResult(ready);
    }

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var options = _monitor.Get(_name);

        using var activity = AuthenticationDiagnostics.Source.StartActivity(
            AuthenticationDiagnostics.ActivityStrategyExecute, ActivityKind.Client);
        activity?.SetTag(AuthenticationDiagnostics.TagStrategyName, _name);

        try
        {
            var scopes = ResolveScopes(options);

            var result = options.Flow switch
            {
                OidcFlow.ClientCredentials     => await AcquireClientCredentialsTokenAsync(options, scopes, cancellationToken),
                OidcFlow.ResourceOwnerPassword => await AcquireRopcTokenAsync(options, scopes, cancellationToken),
                _                              => AuthenticationResult.Failure($"Unsupported OIDC flow: {options.Flow}")
            };

            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, result.IsSuccess);
            activity?.SetTag(AuthenticationDiagnostics.TagSubject,   result.Subject);
            return result;
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "MSAL error during {Flow} flow for strategy '{Name}': {Error}",
                options.Flow, _name, ex.ErrorCode);
            activity?.SetStatus(ActivityStatusCode.Error, ex.ErrorCode);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return AuthenticationResult.Failure($"MSAL error ({ex.ErrorCode}): {ex.Message}", ex);
        }
        catch (AuthenticationException)
        {
            throw; // propagate structured exceptions unchanged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OIDC authentication for strategy '{Name}'", _name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return AuthenticationResult.Failure(ex.Message, ex);
        }
    }

    // -- Private helpers -------------------------------------------------------

    private async Task<AuthenticationResult> AcquireClientCredentialsTokenAsync(
        OidcAuthenticationOptions o,
        IEnumerable<string> scopes,
        CancellationToken cancellationToken)
    {
        var app = BuildConfidentialClient(o);
        MsalAuthResult result = await app.AcquireTokenForClient(scopes)
                                         .ExecuteAsync(cancellationToken);

        _logger.LogDebug("OIDC Client Credentials token acquired for '{Name}'. Expires: {Expiry}",
            _name, result.ExpiresOn);
        return AuthenticationResult.Success(result.AccessToken, "Bearer", result.ExpiresOn, subject: o.ClientId);
    }

    private async Task<AuthenticationResult> AcquireRopcTokenAsync(
        OidcAuthenticationOptions o,
        IEnumerable<string> scopes,
        CancellationToken cancellationToken)
    {
        var app = BuildPublicClient(o);
        MsalAuthResult result = await app
            .AcquireTokenByUsernamePassword(scopes, o.Username!, o.Password!)
            .ExecuteAsync(cancellationToken);

        _logger.LogDebug("OIDC ROPC token acquired for user {User} in strategy '{Name}'. Expires: {Expiry}",
            o.Username, _name, result.ExpiresOn);

        return AuthenticationResult.Success(result.AccessToken, "Bearer", result.ExpiresOn, subject: o.Username);
    }

    private static IConfidentialClientApplication BuildConfidentialClient(OidcAuthenticationOptions o) =>
        ConfidentialClientApplicationBuilder
            .Create(o.ClientId)
            .WithClientSecret(o.ClientSecret)
            .WithAuthority(o.Authority)
            .Build();

    private static IPublicClientApplication BuildPublicClient(OidcAuthenticationOptions o) =>
        PublicClientApplicationBuilder
            .Create(o.ClientId)
            .WithAuthority(o.Authority)
            .Build();

    private static IEnumerable<string> ResolveScopes(OidcAuthenticationOptions options) =>
        options.Scopes?.Any() == true
            ? options.Scopes
            : [$"{options.ClientId}/.default"];
}
