---
layout: default
library: authentication
title: OIDC (OAuth 2.0)
description: Delegate credential verification to any OAuth 2.0 / OpenID Connect provider via MSAL.NET. Supports Client Credentials for machine-to-machine and ROPC for user-delegated access.
permalink: /authentication/strategies/oidc/
---

## Overview

OIDC (OpenID Connect) is the most widely deployed authentication protocol for modern applications. It
builds on top of OAuth 2.0 and adds a standardized identity layer. Nearly every major identity
provider supports it: Azure Active Directory, Okta, Auth0, Google, Keycloak, and many others.

The OIDC strategy uses **MSAL.NET** (`Microsoft.Identity.Client`) to acquire tokens from the
configured authority. MSAL handles:

- The token endpoint discovery (via the OIDC discovery document at `{Authority}/.well-known/openid-configuration`)
- The actual HTTP credential exchange
- Its own internal token cache ΓÇö MSAL reuses access tokens until they expire, avoiding unnecessary round-trips

The strategy supports two grant types:

| Flow | `OidcFlow` value | When to use |
|---|---|---|
| **Client Credentials** | `ClientCredentials` (default) | Machine-to-machine: your service authenticates _as itself_, not on behalf of a user. Use for background jobs, microservice calls, and daemons. |
| **Resource Owner Password** | `ResourceOwnerPassword` | A human userΓÇÖs credentials are passed directly to the token endpoint. Only use when the user cannot be redirected to a login page (e.g. a legacy desktop app). |

---

## Client Credentials (recommended for M2M)

The Client Credentials flow is the correct choice for service-to-service calls. Your application
presents its `ClientId` and `ClientSecret` to the identity provider and receives an access token
that represents _the application_, not any individual user.

```csharp
services.AddAuthentication()
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenantId}/v2.0";
        o.ClientId     = "your-application-client-id";
        o.ClientSecret = configuration["Oidc:ClientSecret"]!;  // keep in Key Vault

        // Optional: scope defaults to ["{ClientId}/.default"] if not set
        o.Scopes = ["https://graph.microsoft.com/.default"];
    });
```

**What the authority URL means:**
- For Azure AD, the authority is `https://login.microsoftonline.com/{tenantId}/v2.0` where `{tenantId}` is your Azure AD tenantΓÇÖs GUID or domain name.
- For other providers (Auth0, Okta), replace with the appropriate base URL from their docs.
- MSAL appends `/.well-known/openid-configuration` automatically to discover the token endpoint.

---

## ROPC (Resource Owner Password Credentials)

<div class="bd-callout bd-callout-warning">
<strong>ROPC is a legacy flow with significant limitations.</strong> The userΓÇÖs credentials are
transmitted to <em>your server</em> and then forwarded to the identity provider ΓÇö the user never
interacts with the IdPΓÇÖs own login page. This breaks the security model that makes OAuth 2.0
trust-worthy: you become responsible for handling the raw password. Multi-factor authentication
and Conditional Access policies cannot be enforced in ROPC flows. Most identity providers are
actively deprecating it. Use authorization-code or device-code flows for user login wherever possible.
</div>

Use ROPC only when a redirect-based flow genuinely cannot be used:

```csharp
.AddOidc(o =>
{
    o.Authority    = "https://login.microsoftonline.com/{tenantId}/v2.0";
    o.ClientId     = "your-app-client-id";
    o.ClientSecret = configuration["Oidc:ClientSecret"]!;
    o.Flow         = OidcFlow.ResourceOwnerPassword;
    o.Username     = configuration["Oidc:Username"]!;
    o.Password     = configuration["Oidc:Password"]!;
});
```

---

## Multiple OIDC registrations

Register several OIDC tenants or providers side-by-side by supplying an explicit name:

```csharp
.AddOidc("AzureAD",  o => { o.Authority = "https://login.microsoftonline.com/contoso/v2.0"; ΓÇª })
.AddOidc("Okta",     o => { o.Authority = "https://contoso.okta.com/oauth2/default"; ΓÇª })
.AddOidc("Internal", o => { o.Authority = "https://auth.internal.example.com"; ΓÇª })
```

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Authority` | `string` | Γ£à | ΓÇö | OIDC discovery base URL. MSAL appends `/.well-known/openid-configuration`. |
| `ClientId` | `string` | Γ£à | ΓÇö | Application (client) ID registered with the identity provider. |
| `ClientSecret` | `string?` | Γ£à for confidential clients | ΓÇö | Client secret. Never commit to source control. |
| `Scopes` | `IEnumerable<string>` | | `["{ClientId}/.default"]` | OAuth2 scopes to request. The `/.default` suffix requests all statically declared permissions. |
| `Flow` | `OidcFlow` | | `ClientCredentials` | Grant type: `ClientCredentials` or `ResourceOwnerPassword`. |
| `Username` | `string?` | ROPC only | ΓÇö | End-user login name. Only used when `Flow = ResourceOwnerPassword`. |
| `Password` | `string?` | ROPC only | ΓÇö | End-user password. Only used when `Flow = ResourceOwnerPassword`. |

---

## Subject claim

The `sub` claim embedded in the issued JWT is set to:

- **Client Credentials** ΓÇö `ClientId` (the applicationΓÇÖs identifier)
- **ROPC** ΓÇö `Username` (the human userΓÇÖs identifier)

---

## Token caching

MSAL.NET maintains its own internal token cache. Access tokens acquired from the identity provider
are reused until they expire, which means calling `AuthenticateAsync("OIDC")` many times in quick
succession only results in one network round-trip. The Primitives cache layer (`EarlyExpiryBuffer`)
sits on top and evicts results 30 seconds before expiry to prevent serving tokens that are about
to expire.

---

## `CanHandleAsync` behaviour

Before calling the identity provider, the strategy checks that required fields are set:
- **Client Credentials**: `Authority`, `ClientId`, and `ClientSecret` must all be non-empty.
- **ROPC**: additionally requires `Username` and `Password`.

If any required field is missing, `CanHandleAsync()` returns `false` and a descriptive failure
result is returned immediately without making any network call. This is useful for health checks.

---

## Strategy name

```
"OIDC"   (or whatever explicit name you passed to .AddOidc("name", o => ΓÇª))
```

---

## Registration

```csharp
services.AddAuthentication()
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenantId}";
        o.ClientId     = "your-app-client-id";
        o.ClientSecret = configuration["Oidc:ClientSecret"]!;

        // Optional ΓÇö defaults to ["{ClientId}/.default"]
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
| `Authority` | `string` | Γ£à | ΓÇö | OAuth2 authority URL |
| `ClientId` | `string` | Γ£à | ΓÇö | Application (client) ID |
| `ClientSecret` | `string?` | Γ£à | ΓÇö | Client secret |
| `Scopes` | `IEnumerable<string>` | | `["{ClientId}/.default"]` | Requested scopes |
| `Flow` | `OidcFlow` | | `ClientCredentials` | Grant type |
| `Username` | `string?` | ROPC only | ΓÇö | End-user login |
| `Password` | `string?` | ROPC only | ΓÇö | End-user password |

---

## Subject claim

The `Subject` populated on `AuthenticationResult` (used as the JWT `sub` claim) is:

- **Client Credentials** ΓåÆ `ClientId`
- **ROPC** ΓåÆ `Username`

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