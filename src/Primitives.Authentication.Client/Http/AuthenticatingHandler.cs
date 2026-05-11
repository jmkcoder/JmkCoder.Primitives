using Microsoft.Extensions.Logging;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Strategies.TokenIssuance;
using System.Net;
using System.Net.Http.Headers;

namespace Primitives.Authentication.Client.Http;

/// <summary>
/// <see cref="DelegatingHandler"/> that acquires a JWT via <see cref="ITokenIssuanceService"/>
/// and attaches it to every outgoing <see cref="HttpRequestMessage"/> as an
/// <c>Authorization: Bearer &lt;token&gt;</c> header.
/// </summary>
/// <remarks>
/// On a <c>401 Unauthorized</c> response the handler automatically attempts one token refresh:
/// <list type="number">
///   <item><description>If a refresh token is available, calls <see cref="ITokenIssuanceService.RefreshAsync"/>.</description></item>
///   <item><description>Otherwise falls back to a full <see cref="ITokenIssuanceService.AuthenticateAsync"/>.</description></item>
///   <item><description>Retries the original request once with the new token.</description></item>
/// </list>
/// Requests with non-buffered (streaming) bodies cannot be retried — the 401 is returned as-is.
///
/// Register via <see cref="HttpClientBuilderExtensions.AddPrimitivesAuthentication"/>.
/// </remarks>
public sealed class AuthenticatingHandler : DelegatingHandler
{
    private readonly ITokenIssuanceService          _tokenService;
    private readonly AuthenticatingHandlerOptions   _options;
    private readonly ILogger<AuthenticatingHandler> _logger;

    // Last successful result — refresh token is stored here for the 401-retry flow.
    // Access is serialised through _lock to prevent thundering-herd refreshes.
    private AuthenticationResult?   _cached;
    private readonly SemaphoreSlim  _lock = new(1, 1);

    public AuthenticatingHandler(
        ITokenIssuanceService          tokenService,
        AuthenticatingHandlerOptions   options,
        ILogger<AuthenticatingHandler> logger)
    {
        _tokenService = tokenService;
        _options      = options;
        _logger       = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken  cancellationToken)
    {
        await AttachTokenAsync(request, cancellationToken).ConfigureAwait(false);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // 401 — attempt one refresh + retry.
        var retryRequest = TryCloneRequest(request);
        if (retryRequest is null)
        {
            _logger.LogWarning(
                "AuthenticatingHandler: received 401 for strategy '{Strategy}' but cannot retry " +
                "a request with a streaming body.",
                _options.StrategyName);
            return response;
        }

        var refreshed = await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!refreshed)
            return response;

        await AttachTokenAsync(retryRequest, cancellationToken).ConfigureAwait(false);
        response.Dispose();
        return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _lock.Dispose();

        base.Dispose(disposing);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Ensures a valid token is cached, then writes it into the request Authorization header.
    /// </summary>
    private async Task AttachTokenAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _cached ??= await _tokenService.AuthenticateAsync(_options.StrategyName, ct)
                                            .ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }

        if (_cached?.IsSuccess == true && _cached.AccessToken is { Length: > 0 } token)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(_options.TokenPrefix, token);
        }
        else
        {
            _logger.LogWarning(
                "AuthenticatingHandler: could not acquire token for strategy '{Strategy}': {Error}",
                _options.StrategyName, _cached?.ErrorMessage);
        }
    }

    /// <summary>
    /// Refreshes (or re-authenticates) and updates <see cref="_cached"/>.
    /// Returns <c>true</c> if a new valid token is now available.
    /// </summary>
    private async Task<bool> RefreshTokenAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            AuthenticationResult result;

            if (_cached?.RefreshToken is { Length: > 0 } rt)
            {
                _logger.LogDebug(
                    "AuthenticatingHandler: 401 received — refreshing token for strategy '{Strategy}'.",
                    _options.StrategyName);
                result = await _tokenService.RefreshAsync(rt, ct).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning(
                        "AuthenticatingHandler: token refresh failed for '{Strategy}': {Error}. " +
                        "Falling back to full re-authentication.",
                        _options.StrategyName, result.ErrorMessage);
                    result = await _tokenService.AuthenticateAsync(_options.StrategyName, ct)
                                                .ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogDebug(
                    "AuthenticatingHandler: 401 received, no refresh token — re-authenticating " +
                    "strategy '{Strategy}'.",
                    _options.StrategyName);
                result = await _tokenService.AuthenticateAsync(_options.StrategyName, ct)
                                            .ConfigureAwait(false);
            }

            if (result.IsSuccess)
            {
                _cached = result;
                return true;
            }

            _logger.LogWarning(
                "AuthenticatingHandler: re-authentication failed for strategy '{Strategy}': {Error}",
                _options.StrategyName, result.ErrorMessage);
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Returns a shallow clone of <paramref name="original"/> suitable for retry, or
    /// <c>null</c> when the request body is a non-buffered (streaming) stream.
    /// </summary>
    private static HttpRequestMessage? TryCloneRequest(HttpRequestMessage original)
    {
        // Only retry when the body is absent or buffered (re-readable).
        if (original.Content is not null
            and not ByteArrayContent
            and not StringContent
            and not FormUrlEncodedContent)
        {
            return null; // streaming body — cannot replay
        }

        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
            Content = original.Content,  // safe: ByteArrayContent / StringContent re-create the stream
        };

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}

