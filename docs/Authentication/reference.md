---
layout: default
library: authentication
title: Configuration Reference
description: Complete options reference for all three packages — JwtOptions, strategy options, cache options, and handler options.
permalink: /authentication/reference/
---

This page is a complete reference for every options class in all three packages. Use the page
search (`Ctrl+F`) to jump directly to the property you need. Each section begins with a brief note
on which builder method populates that options class.

---

## `JwtOptions`

Configured via `.AddJwtTokenIssuance(o => { … })`. These values control both the tokens that are
**issued** (signed with `SigningKey`) and the tokens that are **validated** inbound (if you also
call `AddPrimitivesJwtBearer()`). The `Issuer`, `Audience`, and `SigningKey` must be identical in
both calls.

| Property | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Issuer` | `string` | ✅ | — | Written as the `iss` claim in every issued JWT |
| `Audience` | `string` | ✅ | — | Written as the `aud` claim; validated on inbound tokens |
| `SigningKey` | `string` | ✅ | — | HS256 symmetric key — **minimum 32 characters** |
| `AccessTokenLifetime` | `TimeSpan` | | `00:15:00` | How long each JWT is valid |
| `RefreshTokenLifetime` | `TimeSpan` | | `7.00:00:00` | How long a refresh token can be used before it expires |

---

## `OidcAuthenticationOptions`

Configured via `.AddOidc(o => { … })` or `.AddOidc("name", o => { … })`. See the
[OIDC strategy page]({{ '/authentication/strategies/oidc/' | relative_url }}) for a conceptual overview.

| Property | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Authority` | `string` | ✅ | — | OIDC discovery base URL |
| `ClientId` | `string` | ✅ | — | Application (client) ID |
| `ClientSecret` | `string?` | | `null` | Required for confidential clients |
| `Scopes` | `IEnumerable<string>` | | `["{ClientId}/.default"]` | Requested OAuth2 scopes |
| `Flow` | `OidcFlow` | | `ClientCredentials` | `ClientCredentials` or `ResourceOwnerPassword` |
| `Username` | `string?` | ROPC only | `null` | End-user login (ROPC) |
| `Password` | `string?` | ROPC only | `null` | End-user password (ROPC) |

---

## `UsernamePasswordAuthenticationOptions`

Configured via `.AddUsernamePassword(o => { … })`.

| Property | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Username` | `string` | ✅ | — | Account username |
| `Password` | `string` | ✅ | — | Account password |
| `Realm` | `string?` | | `null` | Optional Basic Auth realm |
| `Encoding` | `Encoding` | | `UTF-8` | Character encoding for credential bytes |

---

## `KerberosAuthenticationOptions`

Configured via `.AddKerberos(o => { … })`.

| Property | Type | Required | Default | Notes |
|---|---|---|---|---|
| `ServicePrincipalName` | `string` | ✅ | — | Target SPN, e.g. `HTTP/host.corp.example.com` |
| `Credential` | `NetworkCredentialOptions?` | | `null` | `null` = use process identity (recommended for Windows services) |
| `Package` | `string` | | `"Kerberos"` | SSPI package name; `"Negotiate"` for NTLM fallback |

**`NetworkCredentialOptions`:**

| Property | Type | Required |
|---|---|---|
| `UserName` | `string` | ✅ |
| `Password` | `string` | ✅ |
| `Domain` | `string?` | |

---

## `ApiKeyAuthenticationOptions`

Configured via `.AddApiKey(o => { … })`.

| Property | Type | Required | Default | Notes |
|---|---|---|---|---|
| `ApiKey` | `string` | ✅ | — | The secret key value |
| `Placement` | `ApiKeyPlacement` | | `Header` | `Header`, `QueryParameter`, or `BearerToken` |
| `KeyName` | `string` | | `"X-API-Key"` | Header name or query parameter name |
| `HeaderPrefix` | `string` | | `""` | Prefix prepended to the value (headers only) |

---

## `AuthenticationCacheOptions`

Configured via `.AddResultCache(o => { … })` or `.AddDistributedResultCache(o => { … })`.

| Property | Type | Default | Notes |
|---|---|---|---|
| `EarlyExpiryBuffer` | `TimeSpan` | `00:00:30` | Cached results are evicted this long before `ExpiresAt` |

---

## `AuthenticatingHandlerOptions`

Configured via `.AddPrimitivesAuthentication(strategyName, tokenPrefix, headerName)` on `IHttpClientBuilder`.

| Property | Default | Notes |
|---|---|---|
| `StrategyName` | *(required)* | Strategy to authenticate with |
| `HeaderName` | `"Authorization"` | HTTP header to write |
| `TokenPrefix` | `"Bearer"` | Prefix written before the token value |

---

## `AuthenticationResult` properties

Returned by `ITokenIssuanceService.AuthenticateAsync` and `ITokenIssuanceService.RefreshAsync`.

| Property | Type | Notes |
|---|---|---|
| `IsSuccess` | `bool` | `true` when authentication succeeded |
| `AccessToken` | `string?` | Signed JWT |
| `TokenType` | `string?` | Always `"Bearer"` |
| `ExpiresAt` | `DateTimeOffset?` | UTC expiry of the access token |
| `RefreshToken` | `string?` | URL-safe random refresh token |
| `Subject` | `string?` | Authenticated subject (e.g. user UPN, service name) |
| `Claims` | `IReadOnlyDictionary<string, string>?` | Extra claims from the strategy |
| `ErrorMessage` | `string?` | Human-readable failure reason when `IsSuccess = false` |

---

## `JwtValidationResult` properties

Returned by `IJwtTokenValidator.ValidateAsync`.

| Property | Type | Notes |
|---|---|---|
| `IsValid` | `bool` | `true` when the token passed all validation checks |
| `Principal` | `ClaimsPrincipal?` | Populated on success |
| `ErrorMessage` | `string?` | Failure reason |

---

## `RefreshTokenRotationResult` properties

Returned by `IRefreshTokenStore.ValidateAndRotateAsync`.

| Property | Type | Notes |
|---|---|---|
| `IsValid` | `bool` | `true` when the token was valid and has been rotated |
| `NewToken` | `string?` | The replacement refresh token |
| `Subject` | `string?` | Subject from the original token entry |
| `ErrorMessage` | `string?` | Failure reason |

---

## Cache key prefixes (distributed)

| Store | Key format |
|---|---|
| `DistributedAuthenticationResultCache` | `prim:auth:{strategyName}` |
| `DistributedRefreshTokenStore` | `prim:rt:{token}` |

---

## Interface quick reference

```csharp
// Core
interface IAuthenticationStrategy
{
    string Name { get; }
    Task<bool> CanHandleAsync(CancellationToken ct = default);
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default);
}

interface ITokenIssuanceService
{
    Task<AuthenticationResult> AuthenticateAsync(string strategyName, CancellationToken ct = default);
    Task<AuthenticationResult> RefreshAsync(string refreshToken, CancellationToken ct = default);
}

interface IJwtTokenValidator
{
    Task<JwtValidationResult> ValidateAsync(string token, CancellationToken ct = default);
}

interface IAuthenticationResultCache
{
    Task<AuthenticationResult?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, AuthenticationResult result, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

interface IRefreshTokenStore
{
    Task<string> GenerateAsync(string subject, CancellationToken ct = default);
    Task<RefreshTokenRotationResult> ValidateAndRotateAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
}

// Client
interface IMessageTokenAttacher
{
    Task<bool> AttachAsync(IDictionary<string, string> headers,
                           string strategyName,
                           CancellationToken ct = default);
}

// Server (MQ)
interface IMessageAuthenticationContext
{
    string? GetToken();
}
```