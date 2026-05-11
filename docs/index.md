---
layout: default
permalink: /
---

<div class="bd-hero">
  <h1>Primitives.Authentication</h1>
  <p class="lead">Plug-and-play JWT authentication for every layer of your .NET&nbsp;8 stack.<br>
  Strategy pattern &bull; OIDC &bull; Kerberos &bull; API Key &bull; HTTP, gRPC, SignalR, MQ</p>
  <div class="bd-install">
    <span class="prompt">$ </span>dotnet add package Primitives.Authentication
  </div>
</div>

## Why Primitives.Authentication?

Most authentication libraries couple your code to a single credential mechanism. `Primitives.Authentication` treats each mechanism as a **swappable strategy**:

- Business logic depends only on `ITokenIssuanceService` — never on Kerberos, MSAL, or API keys directly
- Switch, add, or remove strategies by changing one line in DI registration
- Every strategy automatically issues a **signed JWT** + **rolling refresh token** — no extra work

<div class="bd-feature-grid">
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-puzzle-fill"></i></span>
    <h5>Strategy Pattern</h5>
    <p>OIDC, Username/Password, Kerberos, API Key — or plug in your own.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-key-fill"></i></span>
    <h5>JWT Issuance</h5>
    <p>HS256-signed access tokens and rolling refresh tokens from every strategy.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-layers-fill"></i></span>
    <h5>Transport Adapters</h5>
    <p>HTTP, gRPC, SignalR, and message queue support — client and server side.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-box-fill"></i></span>
    <h5>DI-First</h5>
    <p>Fluent builder on <code>IServiceCollection</code>. Works in any .NET 8 host.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-activity"></i></span>
    <h5>Health Checks</h5>
    <p>Built-in <code>IHealthCheck</code> integration for readiness probes.</p>
  </div>
  <div class="bd-feature-card">
    <span class="icon"><i class="bi bi-arrow-repeat"></i></span>
    <h5>Distributed Cache</h5>
    <p>Swap in-memory stores for Redis or SQL Server in one line.</p>
  </div>
</div>

## Three packages, install only what you need

<div class="bd-package-grid">
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Authentication</div>
    <p>Core strategies, JWT issuance, refresh token store. No ASP.NET Core dependency.</p>
    <div class="install-cmd">dotnet add package<br>Primitives.Authentication</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Authentication.AspNetCore</div>
    <p>REST endpoints, JWT Bearer validation, gRPC interceptor, SignalR hub filter.</p>
    <div class="install-cmd">dotnet add package<br>Primitives.Authentication.AspNetCore</div>
  </div>
  <div class="bd-package-card">
    <div class="pkg-name">Primitives.Authentication.Client</div>
    <p>Outbound HTTP handler, gRPC credentials, SignalR helper, MQ token attacher.</p>
    <div class="install-cmd">dotnet add package<br>Primitives.Authentication.Client</div>
  </div>
</div>

## Quick look

```csharp
// 1. Register strategies + JWT issuance
builder.Services
    .AddAuthentication()
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenant}/v2.0";
        o.ClientId     = config["Oidc:ClientId"]!;
        o.ClientSecret = config["Oidc:ClientSecret"]!;
    })
    .AddJwtTokenIssuance(o =>
    {
        o.Issuer     = "https://myapp.example.com";
        o.Audience   = "https://myapi.example.com";
        o.SigningKey  = config["Jwt:SigningKey"]!;   // ≥ 32 chars
    });

// 2. Expose POST /token, /token/refresh, /token/revoke
app.MapPrimitivesTokenEndpoints();
```

```csharp
// 3. Call it from any service
var result = await tokenService.AuthenticateAsync("OIDC");
console.WriteLine(result.AccessToken);   // eyJhbGci...
console.WriteLine(result.RefreshToken);  // rolling token
```

<a href="{{ '/getting-started/' | relative_url }}" class="btn btn-primary mt-2">Get started <i class="bi bi-arrow-right ms-1"></i></a>
