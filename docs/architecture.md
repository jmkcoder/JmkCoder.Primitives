---
layout: default
title: Architecture
description: Component map, data-flow diagrams, and design decisions behind Primitives.Authentication.
permalink: /architecture/
---

## Component map

```
┌──────────────────────────────────────────────────────────────────┐
│  Primitives.Authentication                                        │
│                                                                   │
│  ┌─────────────────┐   ┌──────────────────────────────────────┐  │
│  │   Abstractions   │   │           Strategies/                │  │
│  │                  │   │                                      │  │
│  │ IAuthentication  │   │  ┌────────────┐  ┌────────────────┐ │  │
│  │   Strategy       │◄──┼──│    Oidc/   │  │UsernamePassword│ │  │
│  │                  │   │  │  Options   │  │   Options      │ │  │
│  │ IAuthentication  │   │  │  Strategy  │  │   Strategy     │ │  │
│  │   Context        │   │  └────────────┘  └────────────────┘ │  │
│  │                  │   │  ┌────────────┐  ┌────────────────┐ │  │
│  │ IAuthentication  │   │  │ Kerberos/  │  │   ApiKey/      │ │  │
│  │  StrategyFactory │   │  │  Options   │  │   Options      │ │  │
│  │                  │   │  │  Strategy  │  │   Strategy     │ │  │
│  │ AuthenticationR- │   │  └────────────┘  └────────────────┘ │  │
│  │   esult          │   │                                      │  │
│  └─────────────────┘   │  ┌──────────────────────────────────┐│  │
│                         │  │        TokenIssuance/            ││  │
│  ┌──────────────────┐   │  │                                  ││  │
│  │    Context/       │   │  │  IJwtTokenService               ││  │
│  │ Authentication-   │   │  │  JwtTokenService                ││  │
│  │   Context         │   │  │                                 ││  │
│  └──────────────────┘   │  │  IRefreshTokenStore              ││  │
│                         │  │  InMemoryRefreshTokenStore       ││  │
│  ┌──────────────────┐   │  │                                  ││  │
│  │    Factory/       │   │  │  ITokenIssuanceService          ││  │
│  │ Authentication-   │   │  │  TokenIssuanceService           ││  │
│  │  StrategyFactory  │   │  └──────────────────────────────────┘│  │
│  └──────────────────┘   └──────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  Extensions/                                                  │ │
│  │  ServiceCollectionExtensions  ·  AuthenticationBuilder        │ │
│  └──────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

---

## Authentication flow

### 1. Credential authentication only (`IAuthenticationContext`)

```
Caller
  │
  └─► IAuthenticationContext.AuthenticateAsync()
            │
            └─► IAuthenticationStrategy.AuthenticateAsync()   (active strategy)
                      │
                      └─► AuthenticationResult
                              ├─ IsSuccess
                              ├─ AccessToken   (raw credential token)
                              ├─ TokenType
                              ├─ ExpiresAt
                              └─ Subject
```

### 2. JWT issuance flow (`ITokenIssuanceService`)

```
Caller
  │
  └─► ITokenIssuanceService.AuthenticateAsync("StrategyName")
            │
            ├─► IAuthenticationStrategyFactory.GetStrategy("StrategyName")
            │         └─► IAuthenticationStrategy.AuthenticateAsync()
            │                   └─► raw AuthenticationResult (IsSuccess, Subject, Claims)
            │
            ├─► IJwtTokenService.GenerateAccessToken(subject, additionalClaims)
            │         └─► signed JWT string + ExpiresAt
            │
            └─► IRefreshTokenStore.GenerateAsync(subject)
                      └─► URL-safe random refresh token string
            │
            └─► AuthenticationResult
                    ├─ AccessToken   (JWT)
                    ├─ RefreshToken  (opaque rotating token)
                    ├─ ExpiresAt     (JWT expiry)
                    └─ Subject
```

### 3. Refresh flow (rolling rotation)

```
Caller
  │
  └─► ITokenIssuanceService.RefreshAsync(oldRefreshToken)
            │
            └─► IRefreshTokenStore.ValidateAndRotateAsync(oldRefreshToken)
                      ├─ Valid & active → revoke old, store new, return subject + newToken
                      └─ Invalid / reused → revoke chain, return failure
            │ (on success)
            ├─► IJwtTokenService.GenerateAccessToken(subject)
            │         └─► new signed JWT
            │
            └─► AuthenticationResult
                    ├─ AccessToken   (new JWT)
                    └─ RefreshToken  (new token — old is permanently revoked)
```

---

## Vertical slice layout

Each strategy is an isolated **vertical slice**:

```
Strategies/
├── Oidc/
│   ├── OidcAuthenticationOptions.cs      ← namespace …Strategies.Oidc
│   └── OidcAuthenticationStrategy.cs
├── UsernamePassword/
│   ├── UsernamePasswordAuthenticationOptions.cs
│   └── UsernamePasswordAuthenticationStrategy.cs
├── Kerberos/
│   ├── KerberosAuthenticationOptions.cs
│   └── KerberosAuthenticationStrategy.cs
├── ApiKey/
│   ├── ApiKeyAuthenticationOptions.cs
│   └── ApiKeyAuthenticationStrategy.cs
└── TokenIssuance/
    ├── JwtOptions.cs
    ├── IJwtTokenService.cs
    ├── JwtTokenService.cs
    ├── IRefreshTokenStore.cs
    ├── RefreshTokenEntry.cs
    ├── InMemoryRefreshTokenStore.cs
    ├── ITokenIssuanceService.cs
    └── TokenIssuanceService.cs
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
| `ITokenIssuanceService` | Orchestrates strategy → JWT + refresh | `TokenIssuanceService` |

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
