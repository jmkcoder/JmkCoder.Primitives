---
layout: default
title: OIDC (OAuth 2.0)
description: OAuth 2.0 / OpenID Connect via MSAL.NET. Supports Client Credentials (M2M) and Resource Owner Password Credentials (ROPC).
permalink: /strategies/oidc/
---

## Supported flows

| Flow | `OidcFlow` value | Use case |
|---|---|---|
| **Client Credentials** | `ClientCredentials` (default) | Machine-to-machine; no user context |
| **Resource Owner Password** | `ResourceOwnerPassword` | Username + password delegated to the token endpoint (ROPC) |

---

## Registration

```csharp
services.AddAuthentication()
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenantId}";
        o.ClientId     = "your-app-client-id";
        o.ClientSecret = configuration["Oidc:ClientSecret"]!;

        // Optional — defaults to ["{ClientId}/.default"]
        o.Scopes = ["https://graph.microsoft.com/.default"];

        // Default: OidcFlow.ClientCredentials
        o.Flow = OidcFlow.ClientCredentials;
    });
```

### ROPC (Resource Owner Password Credentials)

```csharp
.AddOidc(o =>
{
    o.Authority    = "https://login.microsoftonline.com/{tenantId}";
    o.ClientId     = "your-app-client-id";
    o.ClientSecret = configuration["Oidc:ClientSecret"]!;
    o.Flow         = OidcFlow.ResourceOwnerPassword;
    o.Username     = configuration["Oidc:Username"]!;
    o.Password     = configuration["Oidc:Password"]!;
});
```

<div class="bd-callout bd-callout-warning">
<strong>ROPC is a legacy flow.</strong> Use Client Credentials for machine-to-machine scenarios.
Re-evaluate whether a device-code or authorization-code flow is feasible before choosing ROPC.
</div>

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Authority` | `string` | ✅ | — | OAuth2 authority URL |
| `ClientId` | `string` | ✅ | — | Application (client) ID |
| `ClientSecret` | `string?` | ✅ | — | Client secret |
| `Scopes` | `IEnumerable<string>` | | `["{ClientId}/.default"]` | Requested scopes |
| `Flow` | `OidcFlow` | | `ClientCredentials` | Grant type |
| `Username` | `string?` | ROPC only | — | End-user login |
| `Password` | `string?` | ROPC only | — | End-user password |

---

## Subject claim

The `Subject` populated on `AuthenticationResult` (used as the JWT `sub` claim) is:

- **Client Credentials** → `ClientId`
- **ROPC** → `Username`

---

## Token caching

MSAL.NET handles token caching internally. Access tokens are reused until they expire,
avoiding unnecessary network round-trips to the identity provider.

---

## Can handle check

`CanHandleAsync()` returns `false` when any required field is missing:

- Client Credentials: `Authority`, `ClientId`, and `ClientSecret` must be non-empty.
- ROPC: additionally requires `Username` and `Password`.

---

## Strategy name

```
"OIDC"
```

Used with `ITokenIssuanceService.AuthenticateAsync("OIDC")` and `IAuthenticationStrategyFactory.GetStrategy("OIDC")`.
