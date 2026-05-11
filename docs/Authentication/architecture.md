---
layout: default
library: authentication
title: Architecture
description: Component map, data-flow diagrams, and design decisions behind Primitives.Authentication.
permalink: /authentication/architecture/
---

## Component map

```
ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ
Γöé  Primitives.Authentication                                        Γöé
Γöé                                                                   Γöé
Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ   ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ  Γöé
Γöé  Γöé   Abstractions   Γöé   Γöé           Strategies/                Γöé  Γöé
Γöé  Γöé                  Γöé   Γöé                                      Γöé  Γöé
Γöé  Γöé IAuthentication  Γöé   Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ Γöé  Γöé
Γöé  Γöé   Strategy       ΓöéΓùäΓöÇΓöÇΓö╝ΓöÇΓöÇΓöé    Oidc/   Γöé  ΓöéUsernamePasswordΓöé Γöé  Γöé
Γöé  Γöé                  Γöé   Γöé  Γöé  Options   Γöé  Γöé   Options      Γöé Γöé  Γöé
Γöé  Γöé IAuthentication  Γöé   Γöé  Γöé  Strategy  Γöé  Γöé   Strategy     Γöé Γöé  Γöé
Γöé  Γöé   Context        Γöé   Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ Γöé  Γöé
Γöé  Γöé                  Γöé   Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ Γöé  Γöé
Γöé  Γöé IAuthentication  Γöé   Γöé  Γöé Kerberos/  Γöé  Γöé   ApiKey/      Γöé Γöé  Γöé
Γöé  Γöé  StrategyFactory Γöé   Γöé  Γöé  Options   Γöé  Γöé   Options      Γöé Γöé  Γöé
Γöé  Γöé                  Γöé   Γöé  Γöé  Strategy  Γöé  Γöé   Strategy     Γöé Γöé  Γöé
Γöé  Γöé AuthenticationR- Γöé   Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ Γöé  Γöé
Γöé  Γöé   esult          Γöé   Γöé                                      Γöé  Γöé
Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ   Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉΓöé  Γöé
Γöé                         Γöé  Γöé        TokenIssuance/            ΓöéΓöé  Γöé
Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ   Γöé  Γöé                                  ΓöéΓöé  Γöé
Γöé  Γöé    Context/       Γöé   Γöé  Γöé  IJwtTokenService               ΓöéΓöé  Γöé
Γöé  Γöé Authentication-   Γöé   Γöé  Γöé  JwtTokenService                ΓöéΓöé  Γöé
Γöé  Γöé   Context         Γöé   Γöé  Γöé                                 ΓöéΓöé  Γöé
Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ   Γöé  Γöé  IRefreshTokenStore              ΓöéΓöé  Γöé
Γöé                         Γöé  Γöé  InMemoryRefreshTokenStore       ΓöéΓöé  Γöé
Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ   Γöé  Γöé                                  ΓöéΓöé  Γöé
Γöé  Γöé    Factory/       Γöé   Γöé  Γöé  ITokenIssuanceService          ΓöéΓöé  Γöé
Γöé  Γöé Authentication-   Γöé   Γöé  Γöé  TokenIssuanceService           ΓöéΓöé  Γöé
Γöé  Γöé  StrategyFactory  Γöé   Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿΓöé  Γöé
Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ   ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ  Γöé
Γöé                                                                   Γöé
Γöé  ΓöîΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÉ Γöé
Γöé  Γöé  Extensions/                                                  Γöé Γöé
Γöé  Γöé  ServiceCollectionExtensions  ┬╖  AuthenticationBuilder        Γöé Γöé
Γöé  ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ Γöé
ΓööΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÿ
```

---

## Authentication flow

### 1. Credential authentication only (`IAuthenticationContext`)

```
Caller
  Γöé
  ΓööΓöÇΓû║ IAuthenticationContext.AuthenticateAsync()
            Γöé
            ΓööΓöÇΓû║ IAuthenticationStrategy.AuthenticateAsync()   (active strategy)
                      Γöé
                      ΓööΓöÇΓû║ AuthenticationResult
                              Γö£ΓöÇ IsSuccess
                              Γö£ΓöÇ AccessToken   (raw credential token)
                              Γö£ΓöÇ TokenType
                              Γö£ΓöÇ ExpiresAt
                              ΓööΓöÇ Subject
```

### 2. JWT issuance flow (`ITokenIssuanceService`)

```
Caller
  Γöé
  ΓööΓöÇΓû║ ITokenIssuanceService.AuthenticateAsync("StrategyName")
            Γöé
            Γö£ΓöÇΓû║ IAuthenticationStrategyFactory.GetStrategy("StrategyName")
            Γöé         ΓööΓöÇΓû║ IAuthenticationStrategy.AuthenticateAsync()
            Γöé                   ΓööΓöÇΓû║ raw AuthenticationResult (IsSuccess, Subject, Claims)
            Γöé
            Γö£ΓöÇΓû║ IJwtTokenService.GenerateAccessToken(subject, additionalClaims)
            Γöé         ΓööΓöÇΓû║ signed JWT string + ExpiresAt
            Γöé
            ΓööΓöÇΓû║ IRefreshTokenStore.GenerateAsync(subject)
                      ΓööΓöÇΓû║ URL-safe random refresh token string
            Γöé
            ΓööΓöÇΓû║ AuthenticationResult
                    Γö£ΓöÇ AccessToken   (JWT)
                    Γö£ΓöÇ RefreshToken  (opaque rotating token)
                    Γö£ΓöÇ ExpiresAt     (JWT expiry)
                    ΓööΓöÇ Subject
```

### 3. Refresh flow (rolling rotation)

```
Caller
  Γöé
  ΓööΓöÇΓû║ ITokenIssuanceService.RefreshAsync(oldRefreshToken)
            Γöé
            ΓööΓöÇΓû║ IRefreshTokenStore.ValidateAndRotateAsync(oldRefreshToken)
                      Γö£ΓöÇ Valid & active ΓåÆ revoke old, store new, return subject + newToken
                      ΓööΓöÇ Invalid / reused ΓåÆ revoke chain, return failure
            Γöé (on success)
            Γö£ΓöÇΓû║ IJwtTokenService.GenerateAccessToken(subject)
            Γöé         ΓööΓöÇΓû║ new signed JWT
            Γöé
            ΓööΓöÇΓû║ AuthenticationResult
                    Γö£ΓöÇ AccessToken   (new JWT)
                    ΓööΓöÇ RefreshToken  (new token ΓÇö old is permanently revoked)
```

---

## Vertical slice layout

Each strategy is an isolated **vertical slice**:

```
Strategies/
Γö£ΓöÇΓöÇ Oidc/
Γöé   Γö£ΓöÇΓöÇ OidcAuthenticationOptions.cs      ΓåÉ namespace ΓÇªStrategies.Oidc
Γöé   ΓööΓöÇΓöÇ OidcAuthenticationStrategy.cs
Γö£ΓöÇΓöÇ UsernamePassword/
Γöé   Γö£ΓöÇΓöÇ UsernamePasswordAuthenticationOptions.cs
Γöé   ΓööΓöÇΓöÇ UsernamePasswordAuthenticationStrategy.cs
Γö£ΓöÇΓöÇ Kerberos/
Γöé   Γö£ΓöÇΓöÇ KerberosAuthenticationOptions.cs
Γöé   ΓööΓöÇΓöÇ KerberosAuthenticationStrategy.cs
Γö£ΓöÇΓöÇ ApiKey/
Γöé   Γö£ΓöÇΓöÇ ApiKeyAuthenticationOptions.cs
Γöé   ΓööΓöÇΓöÇ ApiKeyAuthenticationStrategy.cs
ΓööΓöÇΓöÇ TokenIssuance/
    Γö£ΓöÇΓöÇ JwtOptions.cs
    Γö£ΓöÇΓöÇ IJwtTokenService.cs
    Γö£ΓöÇΓöÇ JwtTokenService.cs
    Γö£ΓöÇΓöÇ IRefreshTokenStore.cs
    Γö£ΓöÇΓöÇ RefreshTokenEntry.cs
    Γö£ΓöÇΓöÇ InMemoryRefreshTokenStore.cs
    Γö£ΓöÇΓöÇ ITokenIssuanceService.cs
    ΓööΓöÇΓöÇ TokenIssuanceService.cs
```

**Dependency rules:**
- Strategy slices depend only on `Primitives.Authentication.Abstractions` and their own options class.
- `TokenIssuance` depends on `Abstractions` (for `IAuthenticationStrategyFactory`).
- No strategy slice depends on another strategy slice.
- `Extensions/AuthenticationBuilder` depends on all slices (registration only; no runtime coupling).

---

## Key interfaces

| Interface | Responsibility | Default implementation |
|---|---|---|
| `IAuthenticationStrategy` | Verifies identity for one mechanism | (per strategy) |
| `IAuthenticationContext` | Holds the active strategy; delegates calls | `AuthenticationContext` |
| `IAuthenticationStrategyFactory` | Resolves strategies by name | `AuthenticationStrategyFactory` |
| `IJwtTokenService` | Mints signed JWTs | `JwtTokenService` |
| `IRefreshTokenStore` | Stores and rotates refresh tokens | `InMemoryRefreshTokenStore` |
| `ITokenIssuanceService` | Orchestrates strategy ΓåÆ JWT + refresh | `TokenIssuanceService` |

---

## Service lifetimes summary

| Service | Lifetime |
|---|---|
| `IAuthenticationStrategyFactory` | Singleton |
| `IAuthenticationContext` | Transient |
| `IAuthenticationStrategy` (all) | Transient |
| `IJwtTokenService` | Singleton |
| `IRefreshTokenStore` | Singleton |
| `ITokenIssuanceService` | Transient |