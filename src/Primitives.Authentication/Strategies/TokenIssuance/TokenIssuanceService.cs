using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Caching;
using Primitives.Authentication.Diagnostics;

namespace Primitives.Authentication.Strategies.TokenIssuance;

/// <summary>
/// Default implementation of <see cref="ITokenIssuanceService"/>.
/// Delegates identity verification to the named <see cref="IAuthenticationStrategy"/>,
/// then issues a JWT access token and a rolling refresh token.
/// Successful results are optionally cached via <see cref="IAuthenticationResultCache"/>.
/// </summary>
public sealed class TokenIssuanceService : ITokenIssuanceService
{
    private readonly IAuthenticationStrategyFactory _factory;
    private readonly IJwtTokenService               _jwtService;
    private readonly IRefreshTokenStore             _refreshTokenStore;
    private readonly IAuthenticationResultCache?    _cache;
    private readonly ILogger<TokenIssuanceService>  _logger;

    public TokenIssuanceService(
        IAuthenticationStrategyFactory factory,
        IJwtTokenService               jwtService,
        IRefreshTokenStore             refreshTokenStore,
        ILogger<TokenIssuanceService>  logger,
        IAuthenticationResultCache?    cache = null)
    {
        _factory           = factory;
        _jwtService        = jwtService;
        _refreshTokenStore = refreshTokenStore;
        _logger            = logger;
        _cache             = cache;
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> AuthenticateAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        using var activity = AuthenticationDiagnostics.Source.StartActivity(
            AuthenticationDiagnostics.ActivityAuthenticate, ActivityKind.Internal);
        activity?.SetTag(AuthenticationDiagnostics.TagStrategyName, strategyName);

        // Check cache first.
        var cacheKey = string.Concat("auth:", strategyName);
        if (_cache is not null)
        {
            var cached = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Returning cached authentication result for strategy '{Strategy}'", strategyName);
                activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, true);
                return cached;
            }
        }

        var strategy   = _factory.GetStrategy(strategyName);
        var authResult = await strategy.AuthenticateAsync(cancellationToken);

        if (!authResult.IsSuccess)
        {
            activity?.SetStatus(ActivityStatusCode.Error, authResult.ErrorMessage);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return authResult;
        }

        var subject = authResult.Subject ?? strategyName;

        var additionalClaims = authResult.Claims?
            .Select(kvp => new Claim(kvp.Key, kvp.Value));

        var (jwt, expiresAt) = _jwtService.GenerateAccessToken(subject, additionalClaims);
        var refreshToken     = await _refreshTokenStore.GenerateAsync(subject, cancellationToken);

        _logger.LogDebug(
            "Issued JWT + refresh token for subject '{Subject}' via strategy '{Strategy}'. JWT expires: {Expiry}",
            subject, strategyName, expiresAt);

        var issuedResult = AuthenticationResult.Success(
            accessToken:  jwt,
            tokenType:    "Bearer",
            expiresAt:    expiresAt,
            claims:       authResult.Claims,
            subject:      subject,
            refreshToken: refreshToken);

        if (_cache is not null)
            await _cache.SetAsync(cacheKey, issuedResult, cancellationToken);

        activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, true);
        activity?.SetTag(AuthenticationDiagnostics.TagSubject,   subject);
        return issuedResult;
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        using var activity = AuthenticationDiagnostics.Source.StartActivity(
            AuthenticationDiagnostics.ActivityRefresh, ActivityKind.Internal);

        var rotation = await _refreshTokenStore.ValidateAndRotateAsync(refreshToken, cancellationToken);

        if (!rotation.IsValid)
        {
            _logger.LogWarning("Refresh token rotation failed: {Reason}", rotation.ErrorMessage);
            activity?.SetStatus(ActivityStatusCode.Error, rotation.ErrorMessage);
            activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, false);
            return AuthenticationResult.Failure(rotation.ErrorMessage ?? "Invalid refresh token.");
        }

        // The new refresh token is already stored in the store by ValidateAndRotateAsync.
        var (jwt, expiresAt) = _jwtService.GenerateAccessToken(rotation.Subject!);

        _logger.LogDebug(
            "Rotated refresh token for subject '{Subject}'. New JWT expires: {Expiry}",
            rotation.Subject, expiresAt);

        activity?.SetTag(AuthenticationDiagnostics.TagIsSuccess, true);
        activity?.SetTag(AuthenticationDiagnostics.TagSubject,   rotation.Subject);

        return AuthenticationResult.Success(
            accessToken:  jwt,
            tokenType:    "Bearer",
            expiresAt:    expiresAt,
            subject:      rotation.Subject,
            refreshToken: rotation.NewToken);
    }
}