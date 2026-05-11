---
layout: default
title: Strategies
description: Choose one or more authentication mechanisms and register them side-by-side. Every strategy is fully independent.
permalink: /strategies/
---

## What is a strategy?

A strategy is a self-contained implementation of a single credential mechanism. It knows one thing:
how to verify _one specific kind_ of credential and return an `AuthenticationResult` that says
whether it succeeded, who the subject is, and any extra claims to embed in the JWT.

Every strategy implements the same interface:

```csharp
public interface IAuthenticationStrategy
{
    string Name { get; }  // unique identifier, e.g. "OIDC" or "PartnerA"

    // A cheap pre-check: is this strategy properly configured and reachable?
    // Return false here and the library skips AuthenticateAsync entirely.
    Task<bool> CanHandleAsync(CancellationToken ct = default);

    // The actual credential verification. Never throw — return a failure result instead.
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default);
}
```

Because every strategy speaks the same language, the rest of your codebase never needs to import
anything strategy-specific. You depend on `ITokenIssuanceService`, call it by strategy name, and
get a standard JWT back regardless of what happened underneath.

---

## Choosing a strategy

| Strategy | Best for | Page |
|---|---|---|
| [OIDC]({{ '/strategies/oidc/' | relative_url }}) | Azure AD, Okta, Auth0, any OAuth 2.0 IdP — machine-to-machine or user login | [OIDC →]({{ '/strategies/oidc/' | relative_url }}) |
| [Username / Password]({{ '/strategies/username-password/' | relative_url }}) | Internal services, legacy systems, test environments | [Username/Password →]({{ '/strategies/username-password/' | relative_url }}) |
| [Kerberos]({{ '/strategies/kerberos/' | relative_url }}) | Windows domain environments, on-premises services, intranet apps | [Kerberos →]({{ '/strategies/kerberos/' | relative_url }}) |
| [API Key]({{ '/strategies/api-key/' | relative_url }}) | Partner integrations, webhooks, service-to-service calls with a shared secret | [API Key →]({{ '/strategies/api-key/' | relative_url }}) |
| [Custom]({{ '/strategies/custom/' | relative_url }}) | Smart cards, biometrics, OTP, LDAP — anything not covered above | [Custom →]({{ '/strategies/custom/' | relative_url }}) |

---

## Registering strategies

Call `AddAuthentication()` and chain as many strategies as you need. Each registration is independent — order within the chain does not matter for strategy resolution:

```csharp
builder.Services
    .AddAuthentication()         // registers the core infrastructure
    .AddOidc(o => { … })       // registers strategy named "OIDC"
    .AddKerberos(o => { … })   // registers strategy named "Kerberos"
    .AddApiKey(o => { … })     // registers strategy named "ApiKey"
    .AddJwtTokenIssuance(o => { … });  // required to issue JWTs
```

### Named registrations

When you need multiple strategies of the same type (e.g. two different OIDC tenants, or three
different API key partners), pass an explicit name as the first argument:

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc("Internal", o => { o.Authority = "https://login.microsoftonline.com/…"; … })
    .AddOidc("External", o => { o.Authority = "https://accounts.google.com"; … })
    .AddApiKey("PartnerA", o => { o.ApiKey = config["Partners:A:Key"]!; })
    .AddApiKey("PartnerB", o => { o.ApiKey = config["Partners:B:Key"]!; })
    .AddJwtTokenIssuance(o => { … });
```

Then resolve by name at runtime:

```csharp
var result = await tokenService.AuthenticateAsync("PartnerA", ct);
```

---

## Default strategy names

| Builder method | Default name |
|---|---|
| `.AddOidc()` | `"OIDC"` |
| `.AddUsernamePassword()` | `"UsernamePassword"` |
| `.AddKerberos()` | `"Kerberos"` |
| `.AddApiKey()` | `"ApiKey"` |
| `.AddCustomStrategy<T>()` | `T.Name` (whatever your implementation returns) |

Names are **case-insensitive** at resolution time.

---

## What `AuthenticationResult` contains

Every strategy returns an `AuthenticationResult`. The JWT issuance layer uses this to mint the token:

| Property | Type | Description |
|---|---|---|
| `IsSuccess` | `bool` | Whether authentication succeeded |
| `AccessToken` | `string?` | The raw credential result (e.g. Negotiate token, API key). Overwritten by the JWT before being returned to the caller. |
| `Subject` | `string?` | The authenticated identity — becomes the JWT `sub` claim |
| `TokenType` | `string?` | Usually `"Bearer"` |
| `ExpiresAt` | `DateTimeOffset?` | When the raw credential expires |
| `Claims` | `IReadOnlyDictionary<string, string>?` | Extra claims to embed in the JWT |
| `ErrorMessage` | `string?` | Human-readable failure reason when `IsSuccess` is `false` |

---

## The `IAuthenticationStrategyFactory`

If you need to select a strategy at runtime (e.g. based on a per-request claim or tenant ID),
inject `IAuthenticationStrategyFactory` directly:

```csharp
public class MultiTenantLoginService(
    IAuthenticationStrategyFactory factory,
    ITokenIssuanceService           tokenService)
{
    public async Task<string> LoginAsync(string tenantId, CancellationToken ct)
    {
        // Map tenantId to a strategy name stored in config or DB
        var strategyName = tenantId switch
        {
            "contoso" => "Internal",
            "fabrikam" => "External",
            _          => throw new NotSupportedException($"Unknown tenant: {tenantId}")
        };

        var result = await tokenService.AuthenticateAsync(strategyName, ct);
        return result.IsSuccess ? result.AccessToken! : throw new UnauthorizedAccessException();
    }
}
```
