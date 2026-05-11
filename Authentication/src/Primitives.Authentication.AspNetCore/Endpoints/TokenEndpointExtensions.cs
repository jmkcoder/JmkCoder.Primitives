using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.AspNetCore.Endpoints;

// ── Request / response DTOs ──────────────────────────────────────────────────

internal sealed record TokenRequest(string StrategyName);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record RevokeRequest(string RefreshToken);

/// <summary>
/// The JSON body returned by every successful token endpoint.
/// </summary>
public sealed record TokenResponse(
    string           AccessToken,
    string?          RefreshToken,
    string           TokenType,
    DateTimeOffset?  ExpiresAt);

// ── Extension ────────────────────────────────────────────────────────────────

public static class TokenEndpointExtensions
{
    /// <summary>
    /// Maps three minimal-API token endpoints under <paramref name="prefix"/>:
    /// <list type="bullet">
    ///   <item><description><c>POST {prefix}</c> — Authenticate via a named strategy, returns JWT + refresh token.</description></item>
    ///   <item><description><c>POST {prefix}/refresh</c> — Rotate a refresh token, returns new JWT + new refresh token.</description></item>
    ///   <item><description><c>POST {prefix}/revoke</c> — Revoke a refresh token (204 No Content).</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Requires <c>services.AddAuthentication().AddJwtTokenIssuance(...)</c> in DI.
    ///
    /// Example registration:
    /// <code>
    /// app.MapPrimitivesTokenEndpoints();           // mounts at /token (default)
    /// app.MapPrimitivesTokenEndpoints("/auth");    // custom prefix
    /// </code>
    ///
    /// Example <c>POST /token</c> request body:
    /// <code>{ "strategyName": "OIDC" }</code>
    ///
    /// Example <c>POST /token/refresh</c> request body:
    /// <code>{ "refreshToken": "dGhpcyBpcyBhIHRlc3Q..." }</code>
    /// </remarks>
    public static IEndpointRouteBuilder MapPrimitivesTokenEndpoints(
        this IEndpointRouteBuilder app,
        string                     prefix = "/token")
    {
        var group = app.MapGroup(prefix);

        // POST /token
        group.MapPost("", async (
            TokenRequest          req,
            ITokenIssuanceService tokenService,
            CancellationToken     ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.StrategyName))
                return Results.BadRequest(new { error = "strategyName is required." });

            var result = await tokenService.AuthenticateAsync(req.StrategyName, ct)
                                           .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Ok(new TokenResponse(
                    result.AccessToken!,
                    result.RefreshToken,
                    result.TokenType ?? "Bearer",
                    result.ExpiresAt))
                : Results.Problem(
                    detail:     result.ErrorMessage ?? "Authentication failed.",
                    statusCode: StatusCodes.Status401Unauthorized);
        })
        .WithName("token_issue")
        .WithSummary("Authenticate and issue a JWT access token")
        .AllowAnonymous();

        // POST /token/refresh
        group.MapPost("/refresh", async (
            RefreshRequest        req,
            ITokenIssuanceService tokenService,
            CancellationToken     ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.BadRequest(new { error = "refreshToken is required." });

            var result = await tokenService.RefreshAsync(req.RefreshToken, ct)
                                           .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Ok(new TokenResponse(
                    result.AccessToken!,
                    result.RefreshToken,
                    result.TokenType ?? "Bearer",
                    result.ExpiresAt))
                : Results.Problem(
                    detail:     result.ErrorMessage ?? "Token refresh failed.",
                    statusCode: StatusCodes.Status401Unauthorized);
        })
        .WithName("token_refresh")
        .WithSummary("Rotate a refresh token and issue a new JWT access token")
        .AllowAnonymous();

        // POST /token/revoke
        group.MapPost("/revoke", async (
            RevokeRequest    req,
            IRefreshTokenStore store,
            CancellationToken  ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.BadRequest(new { error = "refreshToken is required." });

            await store.RevokeAsync(req.RefreshToken, ct).ConfigureAwait(false);
            return Results.NoContent();
        })
        .WithName("token_revoke")
        .WithSummary("Revoke a refresh token")
        .AllowAnonymous();

        return app;
    }
}
