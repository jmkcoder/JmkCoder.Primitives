---
layout: default
title: Installation
description: Add Primitives.Authentication to any .NET 8 project in under five minutes.
permalink: /getting-started/
---

<div class="bd-callout bd-callout-tip">
<strong>Tip — install only what you need.</strong> The core package has no ASP.NET Core dependency
and can be used in worker services, console apps, Azure Functions, and any other .NET 8 host.
</div>

## Requirements

- .NET 8 or later
- Any host that supports `Microsoft.Extensions.DependencyInjection` (ASP.NET Core, Worker Service, console app, etc.)

---

## Installation

```bash
dotnet add package Primitives.Authentication
```

---

## Registration

Call `AddAuthentication()` on `IServiceCollection`, then chain the strategies you need.  
At minimum you must register **one strategy** and call `AddJwtTokenIssuance()` if you want JWT output.

```csharp
using Primitives.Authentication.Extensions;

// --- minimal: one strategy, JWT issuance ---
builder.Services
    .AddAuthentication()
    .AddUsernamePassword(o =>
    {
        o.Username = "alice";
        o.Password = builder.Configuration["Auth:Password"]!;
    })
    .AddJwtTokenIssuance(o =>
    {
        o.Issuer               = "https://myapp.example.com";
        o.Audience             = "https://myapi.example.com";
        o.SigningKey           = builder.Configuration["Jwt:SigningKey"]!;
        o.AccessTokenLifetime  = TimeSpan.FromMinutes(15);
        o.RefreshTokenLifetime = TimeSpan.FromDays(7);
    });
```

You can chain as many strategies as needed — they are all registered and resolved by name:

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc(o => { /* ... */ })
    .AddUsernamePassword(o => { /* ... */ })
    .AddKerberos(o => { /* ... */ })
    .AddApiKey(o => { /* ... */ })
    .AddJwtTokenIssuance(o => { /* ... */ });
```

---

## Authenticate and receive a JWT

Inject `ITokenIssuanceService` and call `AuthenticateAsync` with the strategy name.

```csharp
using Primitives.Authentication.Strategies.TokenIssuance;

public class LoginService(ITokenIssuanceService tokenService)
{
    public async Task<TokenResponse> LoginAsync(CancellationToken ct)
    {
        var result = await tokenService.AuthenticateAsync("UsernamePassword", ct);

        if (!result.IsSuccess)
            throw new UnauthorizedAccessException(result.ErrorMessage);

        return new TokenResponse
        {
            AccessToken  = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresAt    = result.ExpiresAt!.Value
        };
    }
}
```

Strategy names are **case-insensitive** and match the `Name` property on each strategy:

| Strategy | Name |
|---|---|
| OIDC | `"OIDC"` |
| Username/Password | `"UsernamePassword"` |
| Kerberos | `"Kerberos"` |
| API Key | `"ApiKey"` |

---

## Refresh a token

```csharp
var refreshed = await tokenService.RefreshAsync(oldRefreshToken, ct);

// refreshed.AccessToken  → new signed JWT
// refreshed.RefreshToken → new refresh token (old one is permanently revoked)
```

> **Security note:** The old refresh token is revoked the instant rotation succeeds.  
> If the same token is presented a second time, the entire successor chain is revoked
> (refresh token reuse detection).

---

## Runtime strategy switching (without JWT issuance)

If you only need the raw credential result (no JWT wrapper), inject `IAuthenticationContext`
or `IAuthenticationStrategyFactory` instead.

```csharp
// Switch strategies at runtime
var factory = sp.GetRequiredService<IAuthenticationStrategyFactory>();
authContext.SetStrategy(factory.GetStrategy("Kerberos"));

var rawResult = await authContext.AuthenticateAsync(ct);
// rawResult.AccessToken contains the Negotiate token, not a JWT
```

---

## Configuration via `appsettings.json`

Options classes are standard `IOptions<T>` and bind from configuration:

```json
{
  "Oidc": {
    "Authority":    "https://login.microsoftonline.com/{tenantId}",
    "ClientId":     "your-client-id",
    "ClientSecret": "use-a-secret-manager"
  },
  "Jwt": {
    "Issuer":               "https://myapp.example.com",
    "Audience":             "https://myapi.example.com",
    "SigningKey":           "use-a-secret-manager-at-least-32-chars",
    "AccessTokenLifetime":  "00:15:00",
    "RefreshTokenLifetime": "7.00:00:00"
  }
}
```

```csharp
services
    .AddAuthentication()
    .AddOidc(o => configuration.GetSection("Oidc").Bind(o))
    .AddJwtTokenIssuance(o => configuration.GetSection("Jwt").Bind(o));
```

---

## Next steps

- Explore individual strategy pages for all available options
- Read the [JWT & Refresh Tokens](token-issuance) page for production recommendations
- See [Extending](extending) to add a custom strategy
