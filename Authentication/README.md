# Primitives.Authentication

> **Plug-and-play JWT authentication for every layer of your .NET 8 stack.**

A three-package suite built on the **Strategy pattern**. Pick the authentication mechanism that fits your environment (OIDC, Username/Password, Kerberos, or API Key), wire it up with a fluent builder, and every strategy automatically issues a signed **JWT access token** with a **rolling refresh token**. Transport adapters for HTTP, gRPC, SignalR, and message queues are included — zero boilerplate, zero lock-in.

![Build](https://github.com/your-org/Primitives/actions/workflows/ci.yml/badge.svg)
![NuGet](https://img.shields.io/nuget/v/Primitives.Authentication)
![License](https://img.shields.io/badge/license-MIT-blue)

---

## Table of Contents

- [Packages](#packages)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Authentication Strategies](#authentication-strategies)
  - [OIDC (OAuth 2.0)](#oidc-oauth-20)
  - [Username / Password](#username--password)
  - [Kerberos / Negotiate](#kerberos--negotiate)
  - [API Key](#api-key)
  - [Multiple named strategies](#multiple-named-strategies)
  - [Custom strategy](#custom-strategy)
- [JWT Issuance & Refresh Tokens](#jwt-issuance--refresh-tokens)
- [Server — ASP.NET Core](#server--aspnet-core)
  - [REST token endpoints](#rest-token-endpoints)
  - [JWT Bearer validation](#jwt-bearer-validation)
  - [gRPC interceptor](#grpc-interceptor)
  - [SignalR hub filter](#signalr-hub-filter)
  - [Message queue middleware](#message-queue-middleware)
- [Client — outbound calls](#client--outbound-calls)
  - [HTTP / HttpClient](#http--httpclient)
  - [gRPC client](#grpc-client)
  - [SignalR client](#signalr-client)
  - [Message queue producer](#message-queue-producer)
- [Caching](#caching)
  - [In-memory (default)](#in-memory-default)
  - [Distributed cache (Redis, SQL, …)](#distributed-cache-redis-sql-)
- [Health Checks](#health-checks)
- [Configuration Reference](#configuration-reference)
- [Project Layout](#project-layout)
- [Contributing](#contributing)
- [License](#license)

---

## Packages

| Package | Purpose | NuGet |
|---|---|---|
| `Primitives.Authentication` | Core strategies, JWT issuance, refresh tokens | [![NuGet](https://img.shields.io/nuget/v/Primitives.Authentication)](https://nuget.org/packages/Primitives.Authentication) |
| `Primitives.Authentication.AspNetCore` | Server-side JWT validation, REST endpoints, gRPC interceptor, SignalR hub filter | [![NuGet](https://img.shields.io/nuget/v/Primitives.Authentication.AspNetCore)](https://nuget.org/packages/Primitives.Authentication.AspNetCore) |
| `Primitives.Authentication.Client` | Outbound HTTP handler, gRPC credentials, SignalR helper, MQ token attacher | [![NuGet](https://img.shields.io/nuget/v/Primitives.Authentication.Client)](https://nuget.org/packages/Primitives.Authentication.Client) |

Install only what you need. The core package has no ASP.NET Core dependency and can be used in any .NET host (worker services, console apps, Azure Functions, etc.).

---

## Requirements

- **.NET 8** or later
- Any Microsoft DI host (`IServiceCollection`)
- MSAL-capable identity provider if using the OIDC strategy
- Active Directory / Kerberos infrastructure if using the Kerberos strategy

---

## Installation

**Minimal — core strategy + JWT issuance only:**

```bash
dotnet add package Primitives.Authentication
```

**Server application (API / gRPC / SignalR):**

```bash
dotnet add package Primitives.Authentication
dotnet add package Primitives.Authentication.AspNetCore
```

**Client application (HttpClient / gRPC / SignalR outbound):**

```bash
dotnet add package Primitives.Authentication
dotnet add package Primitives.Authentication.Client
```

**Full stack (server + client in the same project):**

```bash
dotnet add package Primitives.Authentication
dotnet add package Primitives.Authentication.AspNetCore
dotnet add package Primitives.Authentication.Client
```

---

## Quick Start

The following example configures OIDC authentication and issues JWTs in an ASP.NET Core minimal API. Every concept in this README builds on this foundation.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication()                        // Primitives.Authentication
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenantId}/v2.0";
        o.ClientId     = builder.Configuration["Oidc:ClientId"]!;
        o.ClientSecret = builder.Configuration["Oidc:ClientSecret"]!;
    })
    .AddJwtTokenIssuance(o =>
    {
        o.Issuer               = "https://myapp.example.com";
        o.Audience             = "https://myapi.example.com";
        o.SigningKey           = builder.Configuration["Jwt:SigningKey"]!;  // min 32 chars
        o.AccessTokenLifetime  = TimeSpan.FromMinutes(15);
        o.RefreshTokenLifetime = TimeSpan.FromDays(7);
    });

var app = builder.Build();

// Expose POST /token, POST /token/refresh, POST /token/revoke
app.MapPrimitivesTokenEndpoints();

app.Run();
```

That's it. Call `POST /token` with `{ "strategyName": "OIDC" }` and you get back a signed JWT plus a refresh token.

---

## Authentication Strategies

Each strategy is independent. You can register as many as you need. Every strategy is automatically wrapped by `ITokenIssuanceService` to produce a JWT regardless of which mechanism was used to verify identity.

### OIDC (OAuth 2.0)

Supports **Client Credentials** (machine-to-machine) and **Resource Owner Password Credentials** (ROPC — when no browser redirect is possible).

```csharp
// Client Credentials (default)
builder.Services.AddAuthentication()
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenantId}/v2.0";
        o.ClientId     = configuration["Oidc:ClientId"]!;
        o.ClientSecret = configuration["Oidc:ClientSecret"]!;
        o.Scopes       = ["api://my-api/.default"];
    });

// Resource Owner Password (ROPC)
builder.Services.AddAuthentication()
    .AddOidc(o =>
    {
        o.Authority    = "https://login.microsoftonline.com/{tenantId}/v2.0";
        o.ClientId     = configuration["Oidc:ClientId"]!;
        o.ClientSecret = configuration["Oidc:ClientSecret"]!;
        o.Flow         = OidcFlow.ResourceOwnerPassword;
        o.Username     = configuration["Oidc:Username"]!;
        o.Password     = configuration["Oidc:Password"]!;
    });
```

| Option | Type | Required | Default | Description |
|---|---|---|---|---|
| `Authority` | `string` | ✅ | — | OIDC discovery endpoint base URL |
| `ClientId` | `string` | ✅ | — | Application (client) ID |
| `ClientSecret` | `string` | — | `null` | Client secret (not needed for public clients) |
| `Scopes` | `IEnumerable<string>` | — | `[]` | Requested scopes |
| `Flow` | `OidcFlow` | — | `ClientCredentials` | `ClientCredentials` or `ResourceOwnerPassword` |
| `Username` | `string` | ROPC only | `null` | End-user name for ROPC flow |
| `Password` | `string` | ROPC only | `null` | End-user password for ROPC flow |

---

### Username / Password

HTTP Basic-style credential verification. Useful for internal services or legacy systems that accept a username and password at the strategy level.

```csharp
builder.Services.AddAuthentication()
    .AddUsernamePassword(o =>
    {
        o.Username = "service-account";
        o.Password = configuration["BasicAuth:Password"]!;
        o.Realm    = "MyApp";  // optional
    });
```

| Option | Type | Required | Default | Description |
|---|---|---|---|---|
| `Username` | `string` | ✅ | — | Account username |
| `Password` | `string` | ✅ | — | Account password |
| `Realm` | `string?` | — | `null` | Optional realm sent in the challenge |
| `Encoding` | `Encoding` | — | `UTF8` | Character encoding for credential bytes |

---

### Kerberos / Negotiate

Windows-integrated authentication via the `Negotiate` SSPI/GSSAPI package. Works on Windows hosts with a Kerberos realm and, on Linux, with a properly configured `krb5.conf`.

```csharp
builder.Services.AddAuthentication()
    .AddKerberos(o =>
    {
        o.ServicePrincipalName = "HTTP/myservice.corp.example.com";
        // Omit Credential to use the process identity (recommended for Windows services)
        // o.Credential = new() { UserName = "svc-account", Password = "...", Domain = "CORP" };
    });
```

| Option | Type | Required | Default | Description |
|---|---|---|---|---|
| `ServicePrincipalName` | `string` | ✅ | — | Target SPN, e.g. `HTTP/host.domain.com` |
| `Credential` | `NetworkCredentialOptions?` | — | `null` | Explicit credential; `null` = process identity |
| `Package` | `string` | — | `"Kerberos"` | SSPI package name (`"Kerberos"` or `"Negotiate"`) |

`NetworkCredentialOptions` fields: `UserName`, `Password`, `Domain?`.

---

### API Key

Validates a static secret. Three placement modes are supported so you can match whatever convention your upstream service expects.

```csharp
builder.Services.AddAuthentication()
    .AddApiKey(o =>
    {
        o.ApiKey    = configuration["ApiKey:Secret"]!;
        o.Placement = ApiKeyPlacement.Header;   // default
        o.KeyName   = "X-API-Key";              // default
    });
```

| Option | Type | Required | Default | Description |
|---|---|---|---|---|
| `ApiKey` | `string` | ✅ | — | The secret key value |
| `Placement` | `ApiKeyPlacement` | — | `Header` | `Header`, `QueryParameter`, or `BearerToken` |
| `KeyName` | `string` | — | `"X-API-Key"` | Header name or query parameter name |
| `HeaderPrefix` | `string` | — | `""` | Optional prefix prepended to the value in the header |

---

### Multiple Named Strategies

Register several strategies with different names and select one at call time.

```csharp
builder.Services.AddAuthentication()
    .AddOidc("AzureAD", o => { o.Authority = "https://login.microsoftonline.com/..."; /* ... */ })
    .AddOidc("Auth0",   o => { o.Authority = "https://my-tenant.auth0.com/";          /* ... */ })
    .AddApiKey("Internal", o => { o.ApiKey = configuration["ApiKey:Internal"]!; })
    .AddJwtTokenIssuance(o => { /* ... */ });
```

```csharp
// Authenticate using a specific strategy by name
var result = await tokenService.AuthenticateAsync("AzureAD");
var result2 = await tokenService.AuthenticateAsync("Internal");
```

Strategy names are **case-insensitive**. The default name when you call `AddOidc(o => …)` (without a name argument) is `"OIDC"`.

| Strategy | Default Name |
|---|---|
| `AddOidc` | `"OIDC"` |
| `AddUsernamePassword` | `"UsernamePassword"` |
| `AddKerberos` | `"Kerberos"` |
| `AddApiKey` | `"ApiKey"` |

---

### Custom Strategy

Implement `IAuthenticationStrategy` to integrate any provider:

```csharp
public sealed class SmartCardStrategy : IAuthenticationStrategy
{
    public string Name => "SmartCard";

    public Task<bool> CanHandleAsync(CancellationToken ct = default)
        => Task.FromResult(SmartCard.IsPresent());

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default)
    {
        var pin = await SmartCard.ReadPinAsync(ct);
        if (!SmartCard.Verify(pin))
            return AuthenticationResult.Failure("PIN rejected");

        return AuthenticationResult.Success(
            accessToken: SmartCard.GetCertificateThumbprint(),
            subject:     SmartCard.GetSubject());
    }
}
```

Register it:

```csharp
builder.Services.AddAuthentication()
    .AddCustomStrategy<SmartCardStrategy>()
    .AddJwtTokenIssuance(o => { /* ... */ });
```

`ITokenIssuanceService` wraps your strategy result in a JWT automatically — no extra plumbing needed.

---

## JWT Issuance & Refresh Tokens

Call `AddJwtTokenIssuance` once. It wires up the `ITokenIssuanceService` that every strategy funnels through.

```csharp
builder.Services.AddAuthentication()
    .AddOidc(/* ... */)
    .AddJwtTokenIssuance(o =>
    {
        o.Issuer               = "https://myapp.example.com";
        o.Audience             = "https://myapi.example.com";
        o.SigningKey           = configuration["Jwt:SigningKey"]!;  // ≥ 32 chars, keep secret
        o.AccessTokenLifetime  = TimeSpan.FromMinutes(15);
        o.RefreshTokenLifetime = TimeSpan.FromDays(7);
    });
```

**Injecting the service:**

```csharp
public class AuthController(ITokenIssuanceService tokens)
{
    // Issue tokens
    public async Task<IActionResult> Login(string strategyName)
    {
        var result = await tokens.AuthenticateAsync(strategyName);
        if (!result.IsSuccess) return Unauthorized(result.ErrorMessage);
        return Ok(new { result.AccessToken, result.RefreshToken, result.ExpiresAt });
    }

    // Rotate refresh token (rolling window)
    public async Task<IActionResult> Refresh(string refreshToken)
    {
        var result = await tokens.RefreshAsync(refreshToken);
        if (!result.IsSuccess) return Unauthorized();
        return Ok(new { result.AccessToken, result.RefreshToken, result.ExpiresAt });
    }
}
```

**`AuthenticationResult` properties:**

| Property | Type | Description |
|---|---|---|
| `IsSuccess` | `bool` | Whether authentication succeeded |
| `AccessToken` | `string?` | Signed JWT |
| `TokenType` | `string?` | Always `"Bearer"` |
| `ExpiresAt` | `DateTimeOffset?` | Access token expiry |
| `RefreshToken` | `string?` | URL-safe random refresh token |
| `Subject` | `string?` | Authenticated subject (user/service identity) |
| `Claims` | `IReadOnlyDictionary<string, string>?` | Additional claims from the strategy |
| `ErrorMessage` | `string?` | Human-readable failure reason |

**Refresh token rotation:** Each call to `RefreshAsync` invalidates the old token and issues a new one (rolling window). Reuse of an already-rotated token is detected and triggers revocation of the entire chain.

---

## Server — ASP.NET Core

Add the AspNetCore package and call `AddPrimitivesAspNetCoreAuthentication`:

```csharp
// Program.cs
builder.Services
    .AddAuthentication()
    .AddOidc(/* ... */)
    .AddJwtTokenIssuance(/* ... */);

// Must come AFTER AddJwtTokenIssuance
builder.Services.AddPrimitivesAspNetCoreAuthentication();
```

> **Important:** `AddPrimitivesAspNetCoreAuthentication` registers the gRPC interceptor and SignalR hub filter. It requires `ITokenIssuanceService` to already be in the container — always call it after `AddJwtTokenIssuance`.

---

### REST Token Endpoints

Expose three HTTP endpoints with a single call:

```csharp
app.MapPrimitivesTokenEndpoints();            // mounts at /token (default)
app.MapPrimitivesTokenEndpoints("/auth");     // or a custom prefix
```

| Method | Path | Body | Response | Description |
|---|---|---|---|---|
| `POST` | `/token` | `{ "strategyName": "OIDC" }` | `TokenResponse` | Authenticate and issue tokens |
| `POST` | `/token/refresh` | `{ "refreshToken": "…" }` | `TokenResponse` | Rotate refresh token |
| `POST` | `/token/revoke` | `{ "refreshToken": "…" }` | `204 No Content` | Revoke a refresh token |

All three endpoints call `.AllowAnonymous()` — authentication happens at the strategy level, not at the HTTP layer.

**`TokenResponse` shape:**

```json
{
  "accessToken":  "eyJhbGci...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "tokenType":    "Bearer",
  "expiresAt":    "2026-05-10T14:30:00+00:00"
}
```

---

### JWT Bearer Validation

Use `AddPrimitivesJwtBearer` instead of the raw `AddJwtBearer` to get pre-configured HS256 validation and automatic SignalR query-string token support:

```csharp
builder.Services
    .AddAuthentication()
    .AddPrimitivesJwtBearer(o =>
    {
        o.Issuer    = "https://myapp.example.com";
        o.Audience  = "https://myapi.example.com";
        o.SigningKey = configuration["Jwt:SigningKey"]!;
    });

// In the pipeline
app.UseAuthentication();
app.UseAuthorization();
```

This is the standard way to protect your own controllers / minimal API routes against tokens issued by this library.

---

### gRPC Interceptor

`AuthenticationServerInterceptor` validates the `authorization` metadata key on every inbound call type (unary, server-streaming, client-streaming, bidirectional). It throws `RpcException(StatusCode.Unauthenticated)` when the token is missing or invalid.

```csharp
// Server setup (AddPrimitivesAspNetCoreAuthentication registers it automatically)
builder.Services.AddGrpc(o =>
{
    o.Interceptors.Add<AuthenticationServerInterceptor>();
});
```

The interceptor accepts both `Bearer <token>` and a raw token value in the metadata.

---

### SignalR Hub Filter

`AuthenticationHubFilter` validates the JWT on `OnConnectedAsync` and before every hub method invocation. It reads the token from either:

- The `Authorization` header (`Bearer <token>`), or
- The `?access_token=<token>` query parameter (required by browser SignalR clients).

```csharp
builder.Services.AddSignalR(o =>
{
    o.AddFilter<AuthenticationHubFilter>();
});
```

Hub methods receive a populated `Context.User` (`ClaimsPrincipal`) after the filter passes.

---

### Message Queue Middleware

Extend `MessageAuthenticationMiddlewareBase<TContext>` to authenticate inbound messages from any broker (RabbitMQ, Azure Service Bus, Kafka, etc.):

```csharp
public sealed class MyConsumerMiddleware : MessageAuthenticationMiddlewareBase<MyMessageContext>
{
    public override async Task InvokeAsync(MyMessageContext context, Func<Task> next)
    {
        var ok = await AuthenticateAsync(context);   // calls ValidateAsync internally
        if (!ok)
        {
            // reject / dead-letter the message
            return;
        }
        // Principal is now populated
        await next();
    }
}
```

`TContext` must implement `IMessageAuthenticationContext`, which has a single method: `string? GetToken()` — return the raw JWT (without the `Bearer ` prefix) extracted from your message headers.

---

## Client — Outbound Calls

### HTTP / HttpClient

`AuthenticatingHandler` is a `DelegatingHandler` that:

1. Acquires a token via the configured strategy.
2. Attaches it as `Authorization: Bearer <token>` on every request.
3. On a `401 Unauthorized` response, automatically tries to refresh the token (if a refresh token is available) or falls back to a full re-authentication — then retries the original request once.

> **Note:** Retry is only attempted for buffered request bodies (`ByteArrayContent`, `StringContent`, `FormUrlEncodedContent`). Streaming bodies (`StreamContent`) cannot be replayed; a `401` is returned as-is in that case.

**Named `HttpClient` (recommended):**

```csharp
// Registration
builder.Services
    .AddAuthentication()
    .AddApiKey("MyApi", o => { o.ApiKey = configuration["MyApi:Key"]!; })
    .AddJwtTokenIssuance(o => { /* ... */ });

builder.Services
    .AddHttpClient("MyApiClient", c => { c.BaseAddress = new Uri("https://my-api.example.com"); })
    .AddPrimitivesAuthentication(strategyName: "MyApi");
```

```csharp
// Usage
public class MyApiClient(IHttpClientFactory factory)
{
    private readonly HttpClient _http = factory.CreateClient("MyApiClient");

    public Task<MyData> GetDataAsync() =>
        _http.GetFromJsonAsync<MyData>("/data")!;
}
```

**Manual registration (typed client):**

```csharp
builder.Services.AddHttpClient<MyTypedClient>()
    .AddPrimitivesAuthentication(
        strategyName: "OIDC",
        tokenPrefix: "Bearer",    // default
        headerName: "Authorization");  // default
```

**`AuthenticatingHandlerOptions`:**

| Property | Default | Description |
|---|---|---|
| `StrategyName` | *(required)* | Strategy to use for token acquisition |
| `HeaderName` | `"Authorization"` | HTTP header to write the token into |
| `TokenPrefix` | `"Bearer"` | Prefix written before the token value |

---

### gRPC Client

**TLS channel (production):**

```csharp
var credentials = PrimitivesGrpcCredentials.Create(tokenService, strategyName: "OIDC");

var channel = GrpcChannel.ForAddress("https://my-grpc-service.example.com",
    new GrpcChannelOptions { Credentials = credentials });

var client = new MyService.MyServiceClient(channel);
```

**Insecure channel (dev / internal):**

```csharp
var interceptor = PrimitivesGrpcCredentials.CreateInterceptor(tokenService, strategyName: "OIDC");

var channel = GrpcChannel.ForAddress("http://localhost:5000");
var invoker  = channel.Intercept(interceptor);

var client = new MyService.MyServiceClient(invoker);
```

The interceptor injects a fresh `Bearer` token into the call metadata for every call type.

---

### SignalR Client

```csharp
// Option A — configure via connection options
var connection = new HubConnectionBuilder()
    .WithUrl("https://my-hub.example.com/myhub", options =>
    {
        options.UsePrimitivesAuthentication(tokenService, strategyName: "OIDC");
    })
    .Build();

// Option B — fluent builder (convenience wrapper)
var connection = new HubConnectionBuilder()
    .WithPrimitivesAuthentication(
        hubUrl:       "https://my-hub.example.com/myhub",
        tokenService: tokenService,
        strategyName: "OIDC")
    .Build();

await connection.StartAsync();
```

The helper re-acquires a fresh token each time SignalR reconnects, so token rotation is handled automatically.

---

### Message Queue Producer

`IMessageTokenAttacher` writes `Authorization: Bearer <token>` into any `IDictionary<string, string>` header bag — broker-agnostic by design.

```csharp
// Registration (registers IMessageTokenAttacher)
builder.Services
    .AddAuthentication()
    .AddApiKey(/* ... */)
    .AddJwtTokenIssuance(/* ... */);

builder.Services.AddPrimitivesClientAuthentication();
```

```csharp
// Usage in a producer
public class OrderProducer(IMessageTokenAttacher tokenAttacher, IBusPublisher bus)
{
    public async Task PublishAsync(Order order, CancellationToken ct)
    {
        var headers = new Dictionary<string, string>();
        var ok = await tokenAttacher.AttachAsync(headers, strategyName: "ApiKey", ct);
        if (!ok) throw new InvalidOperationException("Could not acquire token for message.");

        await bus.PublishAsync(order, headers, ct);
    }
}
```

---

## Caching

### In-Memory (Default)

Token acquisition results are cached in-process automatically when you call `AddResultCache`:

```csharp
builder.Services.AddAuthentication()
    .AddOidc(/* ... */)
    .AddJwtTokenIssuance(/* ... */)
    .AddResultCache(o =>
    {
        o.EarlyExpiryBuffer = TimeSpan.FromSeconds(30); // default
    });
```

The cache key is derived from the strategy name. Tokens are evicted `EarlyExpiryBuffer` before their stated expiry to avoid using tokens that are about to expire.

### Distributed Cache (Redis, SQL, …)

For multi-instance deployments, replace the in-memory stores with `IDistributedCache`-backed implementations:

```csharp
// Register any IDistributedCache provider first
builder.Services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = configuration.GetConnectionString("Redis");
});

builder.Services.AddAuthentication()
    .AddOidc(/* ... */)
    .AddJwtTokenIssuance(/* ... */)
    .AddDistributedResultCache()        // replaces in-memory auth result cache
    .AddDistributedRefreshTokenStore(); // replaces in-memory refresh token store
```

Both methods replace (not add to) the default in-memory registrations, so calling them without the default setup also works.

> **Known limitation:** The distributed refresh token store supports individual token revocation but **does not** propagate chain revocation across nodes. If chain revocation is required, use the in-memory store behind a sticky session or implement a custom `IRefreshTokenStore`.

**Cache key prefixes:**

| Store | Key prefix |
|---|---|
| Auth result cache | `prim:auth:` |
| Refresh token store | `prim:rt:` |

---

## Health Checks

```csharp
builder.Services.AddAuthentication()
    .AddOidc(/* ... */)
    .AddJwtTokenIssuance(/* ... */)
    .AddHealthCheck(
        name:          "authentication",      // default
        failureStatus: HealthStatus.Degraded, // default
        tags:          ["auth", "ready"]);
```

```csharp
// Expose the health endpoint
app.MapHealthChecks("/healthz");
```

The check calls `CanHandleAsync` on every registered strategy and reports degraded/unhealthy if any strategy cannot reach its upstream provider.

---

## Configuration Reference

### `JwtOptions`

| Property | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Issuer` | `string` | ✅ | — | `iss` claim in issued tokens |
| `Audience` | `string` | ✅ | — | `aud` claim in issued tokens |
| `SigningKey` | `string` | ✅ | — | HS256 secret, minimum 32 characters |
| `AccessTokenLifetime` | `TimeSpan` | — | `15 min` | How long each JWT is valid |
| `RefreshTokenLifetime` | `TimeSpan` | — | `7 days` | Sliding window for refresh tokens |

### `AuthenticationCacheOptions`

| Property | Type | Default | Notes |
|---|---|---|---|
| `EarlyExpiryBuffer` | `TimeSpan` | `30 s` | Cache evicts tokens this long before they expire |

---

## Project Layout

```
Primitives/
├── src/
│   ├── Primitives.Authentication/
│   │   ├── Abstractions/              # IAuthenticationStrategy, AuthenticationResult, all interfaces
│   │   ├── Caching/                   # InMemory + Distributed IAuthenticationResultCache
│   │   ├── Context/                   # AuthenticationContext (runtime strategy switching)
│   │   ├── Extensions/                # AddAuthentication(), AuthenticationBuilder
│   │   ├── Factory/                   # AuthenticationStrategyFactory
│   │   └── Strategies/
│   │       ├── Oidc/                  # OidcAuthenticationOptions, OidcAuthenticationStrategy
│   │       ├── UsernamePassword/      # Options, Strategy
│   │       ├── Kerberos/              # Options, Strategy
│   │       ├── ApiKey/                # Options, Strategy
│   │       └── TokenIssuance/         # ITokenIssuanceService, IJwtTokenService, refresh token stores
│   │
│   ├── Primitives.Authentication.AspNetCore/
│   │   ├── Endpoints/                 # MapPrimitivesTokenEndpoints()
│   │   ├── Extensions/                # AddPrimitivesAspNetCoreAuthentication(), AddPrimitivesJwtBearer()
│   │   ├── Grpc/                      # AuthenticationServerInterceptor
│   │   ├── SignalR/                   # AuthenticationHubFilter
│   │   └── MessageQueue/              # IMessageAuthenticationContext, MessageAuthenticationMiddlewareBase
│   │
│   └── Primitives.Authentication.Client/
│       ├── Http/                      # AuthenticatingHandler, AuthenticatingHandlerOptions
│       ├── Grpc/                      # AuthenticatingClientInterceptor, PrimitivesGrpcCredentials
│       ├── SignalR/                   # SignalRHubConnectionExtensions
│       ├── MessageQueue/              # IMessageTokenAttacher, MessageTokenAttacher
│       └── Extensions/                # AddPrimitivesClientAuthentication(), AddPrimitivesAuthentication() (HttpClient)
│
├── tests/
│   ├── Primitives.Authentication.Tests/           # Core strategy + JWT unit tests (37 tests)
│   └── Primitives.Authentication.Transport.Tests/ # Handler, interceptor, hub filter tests (15 tests)
│
└── .github/workflows/ci.yml          # Build → Test → Pack (on main)
```

---

## Contributing

1. **Fork** the repository and create a feature branch from `develop`.
2. **Build** the solution: `dotnet build`
3. **Run all tests**: `dotnet test`
4. Ensure no new warnings are introduced (`TreatWarningsAsErrors` is enabled).
5. Open a pull request against `develop` with a clear description of the change.

New authentication strategies should follow the vertical-slice convention:

```
src/Primitives.Authentication/Strategies/MyStrategy/
├── MyStrategyOptions.cs     # Inherits nothing; use DataAnnotations for validation
└── MyStrategy.cs            # Implements IAuthenticationStrategy
```

Register the strategy by calling `.AddCustomStrategy<MyStrategy>()` in the builder, or add a named extension method following the pattern of `AddOidc`.

---

## License

MIT — see [LICENSE](LICENSE) for details.
