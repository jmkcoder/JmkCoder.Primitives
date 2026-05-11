---
layout: default
library: authentication
title: JWT & Refresh Tokens
description: ITokenIssuanceService wraps any registered strategy to produce HS256-signed JWTs and rolling refresh tokens.
permalink: /authentication/jwt-tokens/
---

## Registration

```csharp
services.AddAuthentication()
    .AddOidc(o => { /* ... */ })          // or any other strategy
    .AddJwtTokenIssuance(o =>
    {
        o.Issuer               = "https://myapp.example.com";
        o.Audience             = "https://myapi.example.com";
        o.SigningKey           = configuration["Jwt:SigningKey"]!;
        o.AccessTokenLifetime  = TimeSpan.FromMinutes(15);
        o.RefreshTokenLifetime = TimeSpan.FromDays(7);
    });
```

---

## JWT options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Issuer` | `string` | Γ£à | ΓÇö | JWT `iss` claim |
| `Audience` | `string` | Γ£à | ΓÇö | JWT `aud` claim |
| `SigningKey` | `string` | Γ£à | ΓÇö | HS256 symmetric key ΓÇö **must be ΓëÑ 32 characters** |
| `AccessTokenLifetime` | `TimeSpan` | | `00:15:00` | How long the JWT is valid |
| `RefreshTokenLifetime` | `TimeSpan` | | `7.00:00:00` | How long a refresh token lives before expiry |

> **Security:** Store `SigningKey` in a secrets manager (Azure Key Vault, AWS Secrets Manager,
> HashiCorp Vault, .NET User Secrets). Never commit it to source control.

---

## JWT structure

Every access token contains:

| Claim | Value |
|---|---|
| `sub` | The authenticated principal (`AuthenticationResult.Subject`) |
| `iss` | `JwtOptions.Issuer` |
| `aud` | `JwtOptions.Audience` |
| `jti` | A new `Guid` per token (prevents replay) |
| `iat` | Issued-at Unix timestamp |
| `nbf` | Not-before (same as issued-at) |
| `exp` | Expiry Unix timestamp |
| _additional_ | Any extra claims from `AuthenticationResult.Claims` |

Algorithm: **HS256** (HMAC-SHA256).

---

## Authentication flow

```
Caller ΓåÆ ITokenIssuanceService.AuthenticateAsync("StrategyName")
           Γöé
           Γö£ΓöÇ Resolves the named IAuthenticationStrategy via IAuthenticationStrategyFactory
           Γö£ΓöÇ Calls strategy.AuthenticateAsync() to verify the identity
           Γö£ΓöÇ On success: mints JWT via IJwtTokenService
           ΓööΓöÇ Generates refresh token via IRefreshTokenStore
           Γöé
           ΓööΓöÇΓåÆ AuthenticationResult { AccessToken (JWT), RefreshToken, ExpiresAt, Subject }
```

---

## Refresh flow (rolling rotation)

```
Caller ΓåÆ ITokenIssuanceService.RefreshAsync(oldRefreshToken)
           Γöé
           Γö£ΓöÇ IRefreshTokenStore.ValidateAndRotateAsync(oldRefreshToken)
           Γöé     Γö£ΓöÇ Validates token exists and is not expired / revoked
           Γöé     Γö£ΓöÇ Marks old token as revoked
           Γöé     ΓööΓöÇ Stores a new refresh token
           Γö£ΓöÇ Mints a new JWT for the same subject
           ΓööΓöÇΓåÆ AuthenticationResult { AccessToken (new JWT), RefreshToken (new token) }
```

Every refresh call **revokes the old token and issues a new one** ΓÇö rolling rotation.

---

## Refresh token reuse detection

If a previously used (rotated) refresh token is presented again:

1. The store detects it has already been rotated (`IsRevoked = true`, `ReplacedByToken` set).
2. The **entire successor chain** is immediately revoked.
3. A failure result is returned.

This prevents an attacker who stole a leaked refresh token from obtaining new access tokens
after the legitimate user has already rotated.

---

## Replacing the refresh token store

The default `InMemoryRefreshTokenStore` is suitable for **single-instance** deployments and testing.  
For production multi-instance deployments, implement `IRefreshTokenStore` against a shared backing store:

```csharp
public sealed class RedisRefreshTokenStore : IRefreshTokenStore
{
    // ... implement GenerateAsync, ValidateAndRotateAsync, RevokeAsync
}
```

Register it **after** `AddJwtTokenIssuance()`:

```csharp
services.AddAuthentication()
    .AddJwtTokenIssuance(o => { /* ... */ });

// Override the default in-memory store
services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();
```

Because `AddJwtTokenIssuance` uses `TryAddSingleton`, the override wins.

---

## Service lifetimes

| Service | Lifetime | Reason |
|---|---|---|
| `IJwtTokenService` | Singleton | Stateless; `SigningCredentials` is reused |
| `IRefreshTokenStore` | Singleton | Holds token state; must survive across requests |
| `ITokenIssuanceService` | Transient | Lightweight orchestrator; no state |