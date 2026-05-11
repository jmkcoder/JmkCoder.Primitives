---
layout: default
permalink: /
---

<div class="bd-hero">
  <h1>Primitives.Authentication</h1>
  <p class="lead">
    A .NET&nbsp;8 authentication library that unifies every credential mechanism — OIDC, Kerberos,
    API Key, Username/Password — behind a single interface, and wraps each result in a signed JWT
    with a rolling refresh token. Works across HTTP, gRPC, SignalR, and message queues.
  </p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Authentication
  </div>
</div>

## The problem it solves

Most .NET authentication code is tightly coupled to a single credential mechanism. You build a
service that talks to Azure AD today. Next quarter a partner needs API key access. Six months later
an on-premises client requires Kerberos. Each time, you rewrite login logic, change interfaces, and
touch code that should never have known about credentials at all.

`Primitives.Authentication` separates the two concerns that are usually tangled together:

**Credential verification** — _is this token/password/ticket valid?_
Each mechanism is a self-contained **strategy**. You register only the ones you need. Strategies
are fully independent: adding, removing, or replacing one never affects any other.

**Token issuance** — _here is a signed JWT that proves who you are._
Every successful authentication — regardless of which strategy handled it — produces the same
output: an HS256-signed JWT and a rolling refresh token. The rest of your codebase only ever sees
a standard `Bearer` token. It never needs to know whether the user logged in via Azure AD or a
Kerberos ticket.

<div class="bd-feature-grid">
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-puzzle-fill"></i></span>
    <h5>Strategy Pattern</h5>
    <p>OIDC, Username/Password, Kerberos, API Key — or implement <code>IAuthenticationStrategy</code>
    to support any custom mechanism. Register multiple strategies side-by-side; switch between them
    at runtime by name.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-key-fill"></i></span>
    <h5>JWT Issuance</h5>
    <p>Every successful authentication produces an HS256-signed JWT and a cryptographically random
    rolling refresh token — regardless of which strategy performed the verification. Your API always
    speaks standard <code>Bearer</code>.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-layers-fill"></i></span>
    <h5>Transport Adapters</h5>
    <p>First-class support for HTTP (<code>AuthenticatingHandler</code>), gRPC
    (<code>PrimitivesGrpcCredentials</code>), SignalR
    (<code>WithPrimitivesAuthentication</code>), and any message queue
    (<code>IMessageTokenAttacher</code>) — on both client and server.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-box-fill"></i></span>
    <h5>DI-First</h5>
    <p>Fluent builder on <code>IServiceCollection</code>. Fully compatible with
    <code>IOptions&lt;T&gt;</code>, <code>IConfiguration</code>, and any .NET 8 host — no static
    state, no service locator.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-activity"></i></span>
    <h5>Health Checks</h5>
    <p>Built-in <code>IHealthCheck</code> integration calls <code>CanHandleAsync()</code> on each
    registered strategy. Plug straight into Kubernetes readiness probes or Azure health monitors.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-arrow-repeat"></i></span>
    <h5>Distributed Cache</h5>
    <p>Token results and refresh tokens are cached in-memory by default. Swap to Redis, SQL Server,
    or any <code>IDistributedCache</code> provider in one line — no code changes required.</p>
  </div>
</div>

## Three packages, install only what you need

The library is split so you only take on the dependencies you actually use. A console worker that
just needs to authenticate and call another service doesn't need ASP.NET Core. A microservice that
only _receives_ authenticated requests doesn't need the client package.

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Authentication</div>
    <p>The core engine. Contains all four built-in strategies, JWT issuance, the refresh token store,
    in-memory caching, and the health check. <strong>No ASP.NET Core dependency.</strong> Works in
    any .NET 8 host.</p>
    <div class="install-cmd">dotnet add package<br>Primitives.Authentication</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Authentication.AspNetCore</div>
    <p>Server-side middleware for ASP.NET Core hosts. Adds <code>POST /token</code> REST endpoints,
    JWT Bearer validation for <code>[Authorize]</code>, a gRPC server interceptor, and a SignalR
    hub filter. Depends on the core package automatically.</p>
    <div class="install-cmd">dotnet add package<br>Primitives.Authentication.AspNetCore</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Authentication.Client</div>
    <p>Outbound token injection for services that call other protected APIs. Provides
    <code>AuthenticatingHandler</code> (HttpClient), <code>PrimitivesGrpcCredentials</code>,
    <code>WithPrimitivesAuthentication</code> (SignalR), and <code>IMessageTokenAttacher</code>
    (message queues).</p>
    <div class="install-cmd">dotnet add package<br>Primitives.Authentication.Client</div>
  </div>
</div>

## Quick look

This is everything you need for a fully working authentication API — strategies registered,
tokens issued, endpoints exposed, and the API protected:

```csharp
// Program.cs — register strategies and JWT issuance
builder.Services
    .AddAuthentication()
    .AddOidc(o =>                                   // verify credentials via Azure AD
    {
        o.Authority    = "https://login.microsoftonline.com/{tenant}/v2.0";
        o.ClientId     = config["Oidc:ClientId"]!;
        o.ClientSecret = config["Oidc:ClientSecret"]!;
    })
    .AddJwtTokenIssuance(o =>                       // wrap results in signed JWTs
    {
        o.Issuer     = "https://myapp.example.com";
        o.Audience   = "https://myapi.example.com";
        o.SigningKey  = config["Jwt:SigningKey"]!;   // ≥ 32 chars, keep in Key Vault
    })
    .AddPrimitivesJwtBearer();                      // validate inbound Bearer tokens

// Expose POST /token, /token/refresh, /token/revoke
app.MapPrimitivesTokenEndpoints();

// Protect any route with standard [Authorize] or RequireAuthorization()
app.MapGet("/orders", (ClaimsPrincipal user) => Results.Ok(GetOrders(user)))
   .RequireAuthorization();
```

```csharp
// Any service — inject ITokenIssuanceService and call by strategy name
var result = await tokenService.AuthenticateAsync("OIDC");

if (result.IsSuccess)
{
    Console.WriteLine(result.AccessToken);   // eyJhbGci... (HS256 JWT, 15 min)
    Console.WriteLine(result.RefreshToken);  // cryptographically random, 7 days
    Console.WriteLine(result.ExpiresAt);     // 2026-05-10T15:00:00Z
}
```

```bash
# Or call it over HTTP — no code required on the client side
curl -X POST https://myapp.example.com/token \
     -H "Content-Type: application/json" \
     -d '{"strategyName":"OIDC"}'
```

<a href="{{ '/getting-started/' | relative_url }}" class="btn btn-primary mt-2">Get started <i class="bi bi-arrow-right ms-1"></i></a>
