---
layout: default
library: authentication
title: Extending
description: Adding a new authentication mechanism is a matter of creating one vertical slice and registering it with the builder.
permalink: /authentication/extending/
---

## 1. Create the vertical slice folder

```
src/Primitives.Authentication/Strategies/
ΓööΓöÇΓöÇ MyCustom/
    Γö£ΓöÇΓöÇ MyCustomAuthenticationOptions.cs
    ΓööΓöÇΓöÇ MyCustomAuthenticationStrategy.cs
```

---

## 2. Define the options class

```csharp
// Strategies/MyCustom/MyCustomAuthenticationOptions.cs
using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.MyCustom;

public sealed class MyCustomAuthenticationOptions
{
    [Required]
    public string EndpointUrl { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}
```

---

## 3. Implement the strategy

```csharp
// Strategies/MyCustom/MyCustomAuthenticationStrategy.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Strategies.MyCustom;

public sealed class MyCustomAuthenticationStrategy : IAuthenticationStrategy
{
    private readonly MyCustomAuthenticationOptions _options;
    private readonly ILogger<MyCustomAuthenticationStrategy> _logger;

    // Strategy name used with ITokenIssuanceService.AuthenticateAsync("MyCustom")
    public string Name => "MyCustom";

    public MyCustomAuthenticationStrategy(
        IOptions<MyCustomAuthenticationOptions> options,
        ILogger<MyCustomAuthenticationStrategy> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public Task<bool> CanHandleAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(_options.EndpointUrl) &&
            !string.IsNullOrWhiteSpace(_options.Token));

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: call your identity provider, validate credentials, etc.
            // For this example we treat the configured token as the credential.
            await Task.CompletedTask; // replace with real async work

            _logger.LogDebug("MyCustom authentication succeeded");

            return AuthenticationResult.Success(
                accessToken: _options.Token,
                tokenType:   "Bearer",
                subject:     "custom-principal");  // set to the authenticated identity
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MyCustom authentication failed");
            return AuthenticationResult.Failure(ex.Message, ex);
        }
    }
}
```

---

## 4. Register via the builder

Add an extension method on `AuthenticationBuilder` inside your slice
(or call `AddCustomStrategy<T>()` directly):

### Option A ΓÇö use the built-in helper

```csharp
services.AddAuthentication()
    .AddCustomStrategy<MyCustomAuthenticationStrategy>();

// Configure options separately
services.Configure<MyCustomAuthenticationOptions>(o =>
{
    o.EndpointUrl = "https://my-idp.example.com";
    o.Token       = configuration["MyCustom:Token"]!;
});
```

### Option B ΓÇö add a dedicated extension method (recommended for reusability)

```csharp
// Strategies/MyCustom/MyCustomAuthenticationBuilderExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Extensions;

namespace Primitives.Authentication.Strategies.MyCustom;

public static class MyCustomAuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddMyCustom(
        this AuthenticationBuilder builder,
        Action<MyCustomAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.Configure(configure);
        builder.Services.AddTransient<IAuthenticationStrategy, MyCustomAuthenticationStrategy>();
        return builder;
    }
}
```

```csharp
// Usage
services.AddAuthentication()
    .AddMyCustom(o =>
    {
        o.EndpointUrl = "https://my-idp.example.com";
        o.Token       = configuration["MyCustom:Token"]!;
    })
    .AddJwtTokenIssuance(o => { /* ... */ });
```

---

## 5. Use it

```csharp
var result = await tokenService.AuthenticateAsync("MyCustom", ct);
```

---

## Checklist

- [ ] Options class in `Strategies/MyCustom/`
- [ ] Strategy class in `Strategies/MyCustom/` implementing `IAuthenticationStrategy`
- [ ] `Name` property returns a unique, stable string
- [ ] `CanHandleAsync` checks all required options
- [ ] `AuthenticateAsync` sets `Subject` on `AuthenticationResult.Success`
- [ ] Registered with `AddCustomStrategy<T>()` or a dedicated extension method

---

## Guidelines

- **Keep the slice self-contained.** Options and strategy live together; no cross-slice imports.
- **Set `Subject`** in the success result ΓÇö it becomes the JWT `sub` claim.
- **Never throw** from `AuthenticateAsync`; return `AuthenticationResult.Failure(...)` instead.
- **Log errors** but do not include sensitive credential values in log messages.