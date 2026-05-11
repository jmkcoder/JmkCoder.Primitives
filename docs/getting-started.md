---
layout: default
title: Getting Started
description: Install Primitives.Authentication and issue your first JWT — with a plain-English explanation of what is happening at every step.
permalink: /getting-started/
---

## What you are building

Before writing a line of code, it helps to understand the shape of the system.

When a client (a browser, a mobile app, another service) wants to access a protected resource, it needs to prove its identity. The most common modern approach is to exchange credentials for a short-lived **JSON Web Token (JWT)** that the server validates on every subsequent request.

`Primitives.Authentication` handles both sides:

1. **Credential verification — the strategy.** The strategy knows how to validate one specific kind of credential: an OAuth 2.0 token from Azure AD, a username/password pair, a Kerberos ticket, or an API key. You register the strategies your app needs at startup. The library calls the right one by name.

2. **JWT issuance.** Once a credential is verified, the library wraps the result in a signed JWT and a rolling refresh token. Everything downstream only ever sees a standard `Bearer` token — it never needs to know _which_ credential mechanism was used.

Here is the full flow, from login to protected API call:

```
Client                      Your App                   Identity Provider
  │                            │                               │
  ├── POST /token ───────────►│                               │
  │   { strategyName:"OIDC" }  ├── verify credential ─────────►│
  │                            │◄── confirmed ────────────────│
  │                            │  ┌────────────────────┐   │
  │                            │  │ Sign JWT (HS256)     │   │
  │                            │  │ Issue refresh token  │   │
  │                            │  └────────────────────┘   │
  │◄── 200 { accessToken,     │                               │
  │         refreshToken }     │                               │
  │                            │                               │
  ├── GET /protected ─────────►│                               │
  │   Authorization: Bearer …  │ validate signature (no I/O)   │
  │◄── 200 OK ────────────────│                               │
```

Once the client has its JWT, subsequent requests are validated locally by checking the cryptographic signature — no network call to any identity provider is needed. This is one of the key performance advantages of JWTs.

---

## Requirements

- **.NET 8 or later.** The library targets `net8.0`. It does not support .NET Framework or .NET Standard.
- **Any host that supports `Microsoft.Extensions.DependencyInjection`** — ASP.NET Core minimal APIs, MVC, Worker Services, Azure Functions, and plain console apps all work.

You do _not_ need ASP.NET Core for the core package. The REST endpoints, gRPC interceptor, and SignalR hub filter are in a separate `AspNetCore` package and are entirely optional.

---

## Installation

There are three packages. Install only the ones your project needs.

### Core — required

The core package contains all four built-in strategies, JWT issuance, the refresh token store, and in-memory caching. It has **no ASP.NET Core dependency**, so it works in any .NET 8 host.

```bash
dotnet add package Primitives.Authentication
```

### Server — ASP.NET Core only

Add this if you want to expose `POST /token` HTTP endpoints, protect routes with `[Authorize]`, or use the built-in gRPC interceptor or SignalR hub filter.

```bash
dotnet add package Primitives.Authentication.AspNetCore
```

### Client — for services that call other protected APIs

Add this to services that need to _attach_ tokens to outbound requests — HTTP, gRPC, SignalR, or message queues.

```bash
dotnet add package Primitives.Authentication.Client
```

---

## Step 1 — Register a strategy

Open your `Program.cs` (or `Startup.cs`) and chain the credential strategies you need on `AddAuthentication()`:

```csharp
using Primitives.Authentication.Extensions;

builder.Services
    .AddAuthentication()
    .AddUsernamePassword(o =>
    {
        o.Username = "alice";
        o.Password = builder.Configuration["Auth:Password"]!;  // never hardcode
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

**What each call does:**

- **`AddAuthentication()`** — registers the strategy factory (`IAuthenticationStrategyFactory`), the token issuance service (`ITokenIssuanceService`), in-memory caches, and the refresh token store. This is the library’s DI root. It does not conflict with ASP.NET Core’s own `AddAuthentication()` — they operate at different layers.

- **`AddUsernamePassword(o => …)`** — registers a named strategy called `"UsernamePassword"`. When called, it encodes the configured `Username:Password` as Base-64 and validates the credential. The plain-text password is kept only in memory and never written to disk.

- **`AddJwtTokenIssuance(o => …)`** — tells the library how to produce JWTs after a successful authentication:
  - `Issuer` and `Audience` are embedded in every JWT as the `iss` and `aud` claims. Your validation middleware must expect the same values, or tokens will be rejected.
  - `SigningKey` is the HMAC-SHA256 secret used to sign tokens. It must be at least 32 characters. **Store it in a secrets manager** — Azure Key Vault, AWS Secrets Manager, or `dotnet user-secrets` in development. Anyone with this key can forge valid tokens.
  - `AccessTokenLifetime` controls how long a JWT is valid. 15 minutes is a common production value — short enough that a leaked token has limited usefulness.
  - `RefreshTokenLifetime` controls how long the client can exchange refresh tokens for new access tokens without re-logging in.

<div class="bd-callout bd-callout-tip">
<strong>Bind from appsettings.json.</strong> All options classes support standard <code>IConfiguration</code> binding:
<pre><code class="language-csharp">services
    .AddAuthentication()
    .AddUsernamePassword(o =&gt; configuration.GetSection("Auth").Bind(o))
    .AddJwtTokenIssuance(o =&gt; configuration.GetSection("Jwt").Bind(o));
</code></pre>
</div>

---

## Step 2 — Issue a JWT

Inject `ITokenIssuanceService` into any class that needs to authenticate a user or service, and call `AuthenticateAsync` with the name of the strategy you registered:

```csharp
using Primitives.Authentication.Strategies.TokenIssuance;

public class LoginService(ITokenIssuanceService tokenService)
{
    public async Task<TokenResponse> LoginAsync(CancellationToken ct = default)
    {
        var result = await tokenService.AuthenticateAsync("UsernamePassword", ct);

        if (!result.IsSuccess)
        {
            // result.ErrorMessage is a human-readable failure reason.
            // Log it, but be careful about what you return to the client —
            // avoid leaking which part of the credential was wrong.
            throw new UnauthorizedAccessException(result.ErrorMessage);
        }

        return new TokenResponse
        {
            AccessToken  = result.AccessToken!,    // signed HS256 JWT
            RefreshToken = result.RefreshToken!,   // cryptographically random
            ExpiresAt    = result.ExpiresAt!.Value // UTC expiry
        };
    }
}
```

What `AuthenticateAsync` does internally, step by step:

1. Looks up the strategy named `"UsernamePassword"` via `IAuthenticationStrategyFactory`.
2. Calls `strategy.CanHandleAsync()` — a cheap pre-check that returns `false` if required options are missing (e.g. no password configured). If it returns `false`, authentication is skipped and a failure result is returned immediately.
3. Calls `strategy.AuthenticateAsync()` — the actual credential verification.
4. On success, builds a JWT with `iss`, `aud`, `sub`, `iat`, `exp`, and any strategy-specific claims.
5. Signs the JWT with HS256 using the configured `SigningKey`.
6. Generates a cryptographically random 256-bit refresh token and stores it with the configured expiry.
7. Caches the result in-memory. Subsequent calls within the same token window return the cached value without re-verifying credentials.

Strategy names are **case-insensitive**. `"UsernamePassword"` and `"usernamepassword"` resolve to the same strategy.

---

## Step 3 — Refresh a token

Access tokens are short-lived by design. When one expires, the client exchanges its refresh token for a fresh pair without re-entering credentials. This is the standard OAuth 2.0 refresh token flow:

```csharp
var refreshed = await tokenService.RefreshAsync(expiredRefreshToken, ct);

if (!refreshed.IsSuccess)
{
    // The refresh token has expired, been revoked, or was already rotated.
    // The only recovery is to send the user back through the full login flow.
    return Unauthorized();
}

// The new access token — the 15-minute window starts fresh
var newAccessToken  = refreshed.AccessToken!;

// The new refresh token — the old one is permanently invalid from this point
var newRefreshToken = refreshed.RefreshToken!;
```

**Refresh token rotation** is applied on every refresh: the old token is revoked the instant the new one is issued. This is a security requirement — if a token is somehow intercepted, the attacker can only use it once before the legitimate client rotates it.

**Reuse detection:** if a client presents a refresh token that has _already been rotated_ (a sign that it was stolen and used by an attacker before the legitimate client could rotate it), the library immediately revokes the entire chain of successor tokens. The attacker’s session ends, and the user must log in again. This behaviour follows [RFC 9700 — OAuth 2.0 Security Best Current Practice](https://datatracker.ietf.org/doc/html/rfc9700).

---

## Step 4 — Expose REST endpoints (ASP.NET Core only)

If your host is an ASP.NET Core app, you can expose the full token API — issue, refresh, revoke — with a single call and no custom controllers:

```csharp
// After app.UseAuthentication() and app.UseAuthorization()
app.MapPrimitivesTokenEndpoints();
```

This mounts three routes:

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/token` | Authenticate and receive a JWT + refresh token |
| `POST` | `/token/refresh` | Exchange a refresh token for a new pair |
| `POST` | `/token/revoke` | Invalidate a refresh token immediately |

Example — authenticate from a shell:

```bash
curl -X POST https://myapp.example.com/token \
     -H "Content-Type: application/json" \
     -d '{"strategyName":"UsernamePassword"}'
```

```json
{
  "accessToken":  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9…",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4",
  "tokenType":    "Bearer",
  "expiresAt":    "2026-05-10T15:00:00+00:00"
}
```

All three endpoints call `.AllowAnonymous()` internally — the credential check happens inside the strategy, not at the HTTP middleware layer.

<div class="bd-callout bd-callout-danger">
<strong>Apply rate limiting to <code>POST /token</code>.</strong> Without it, the endpoint can be
used for credential stuffing — automated attempts to guess passwords at scale. Use
<code>builder.Services.AddRateLimiter()</code> and apply a fixed-window or sliding-window policy
to the token endpoint in production.
</div>

---

## Step 5 — Protect your API routes (ASP.NET Core only)

To validate inbound JWTs on protected routes, add JWT Bearer validation with the same signing parameters:

```csharp
builder.Services
    .AddAuthentication()
    .AddJwtTokenIssuance(o => { … })
    .AddPrimitivesJwtBearer();  // uses the same Issuer, Audience, SigningKey
```

```csharp
// Minimal API — only accepts requests with a valid Bearer token
app.MapGet("/orders", (ClaimsPrincipal user) =>
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Results.Ok(GetOrders(subject));
})
.RequireAuthorization();

// Controller — same effect via attribute
[Authorize]
public IActionResult GetOrders() => Ok(GetOrders(User.Identity!.Name));
```

During validation the library checks:
- **Signature** — the JWT was signed with the configured `SigningKey`
- **Expiry** — the `exp` claim has not passed
- **Issuer** — the `iss` claim matches `JwtOptions.Issuer`
- **Audience** — the `aud` claim matches `JwtOptions.Audience`

No network call is made during validation. The signature check is entirely local.

---

## Registering multiple strategies

You can register as many strategies as you like. They are completely independent — each gets its own name and its own configuration block:

```csharp
builder.Services
    .AddAuthentication()
    .AddOidc("AzureAD", o =>           // for internal users — Azure AD
    {
        o.Authority    = "https://login.microsoftonline.com/{tenant}/v2.0";
        o.ClientId     = config["AzureAD:ClientId"]!;
        o.ClientSecret = config["AzureAD:ClientSecret"]!;
    })
    .AddApiKey("PartnerA", o =>        // for external partner A
    {
        o.ApiKey = config["Partners:A:Key"]!;
    })
    .AddApiKey("PartnerB", o =>        // for external partner B
    {
        o.ApiKey = config["Partners:B:Key"]!;
    })
    .AddJwtTokenIssuance(o => { … });
```

Choose which strategy to use at the point of authentication:

```csharp
// In a minimal API handler — pick strategy based on request header
app.MapPost("/token", async (HttpContext ctx, ITokenIssuanceService tokens) =>
{
    var strategyName = ctx.Request.Headers["X-Auth-Strategy"].FirstOrDefault()
                       ?? "AzureAD";

    var result = await tokens.AuthenticateAsync(strategyName);
    return result.IsSuccess ? Results.Ok(result) : Results.Unauthorized();
});
```

---

## Runtime strategy switching (advanced)

If you need to select a strategy dynamically — for example, based on a per-request claim or a
tenant configuration stored in a database — inject `IAuthenticationStrategyFactory` directly:

```csharp
var factory  = sp.GetRequiredService<IAuthenticationStrategyFactory>();
var strategy = factory.GetStrategy("Kerberos");

var rawResult = await strategy.AuthenticateAsync(ct);
// rawResult.AccessToken is the Negotiate token — NOT a JWT
// To wrap it in a JWT, pass the result to ITokenIssuanceService instead
```

---

## What’s next

- **[Strategies]({{ '/strategies/' | relative_url }})** — understand when to use OIDC vs API Key vs Kerberos
- **[Token Endpoints]({{ '/server/token-endpoints/' | relative_url }})** — full route reference and security hardening
- **[JWT Bearer Validation]({{ '/server/jwt-bearer/' | relative_url }})** — protect your controllers and minimal API routes
- **[HTTP Client]({{ '/client/http/' | relative_url }})** — auto-attach tokens to every outbound `HttpClient` request
- **[Caching]({{ '/caching/' | relative_url }})** — swap to Redis for multi-instance deployments

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
