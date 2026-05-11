---
layout: default
title: Custom Strategy
description: Extend Primitives.Authentication with any credential mechanism by implementing IAuthenticationStrategy.
permalink: /strategies/custom/
---

## Overview

The four built-in strategies cover the most common credential mechanisms. But authentication is a\nspace where one size never fits all. Some scenarios that require a custom strategy:\n\n- **Smart card / PIV card** \u2014 the credential is a certificate on a hardware token, verified by\n  PKCS#11 or the Windows CryptoAPI.\n- **Certificate-based (mTLS)** \u2014 the client presents a TLS client certificate, and your strategy\n  validates its thumbprint or subject against an allowlist.\n- **One-Time Password (OTP)** \u2014 the credential includes a TOTP code alongside a username.\n- **SAML assertion** \u2014 the identity provider sends an XML assertion rather than an OAuth token.\n- **LDAP** \u2014 you need to bind against an on-premises Active Directory or OpenLDAP directory.\n- **Biometric** \u2014 the credential is a biometric hash verified by a local hardware reader.\n\nIn every case you need **two files** \u2014 an options class and the strategy implementation \u2014 and a\nsingle DI registration call. Everything else (JWT issuance, caching, health checks, refresh token\nrotation) works automatically without any additional code in your strategy.

---

## 1. Create the options class

```csharp
// src/Primitives.Authentication/Strategies/SmartCard/SmartCardOptions.cs
using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.SmartCard;

public sealed class SmartCardOptions
{
    [Required]
    public string ReaderName { get; set; } = string.Empty;

    public int PinTimeoutSeconds { get; set; } = 30;
}
```

Use `System.ComponentModel.DataAnnotations` attributes — the builder validates options with `ValidateDataAnnotations()` automatically.

---

## 2. Implement the strategy

```csharp
// src/Primitives.Authentication/Strategies/SmartCard/SmartCardStrategy.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Primitives.Authentication.Abstractions;

namespace Primitives.Authentication.Strategies.SmartCard;

public sealed class SmartCardStrategy : IAuthenticationStrategy
{
    private readonly SmartCardOptions    _options;
    private readonly ILogger<SmartCardStrategy> _logger;

    // Use IOptionsMonitor<T> for named registrations, IOptions<T> for unnamed.
    public SmartCardStrategy(
        IOptionsMonitor<SmartCardOptions> monitor,
        ILogger<SmartCardStrategy>        logger)
    {
        _options = monitor.Get(Name);   // resolve the named instance
        _logger  = logger;
    }

    public string Name => "SmartCard";

    public Task<bool> CanHandleAsync(CancellationToken ct = default)
        => Task.FromResult(SmartCardReader.IsPresent(_options.ReaderName));

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default)
    {
        try
        {
            var certificate = await SmartCardReader.ReadCertificateAsync(
                _options.ReaderName, _options.PinTimeoutSeconds, ct);

            return AuthenticationResult.Success(
                accessToken: certificate.Thumbprint,
                subject:     certificate.SubjectName.Name,
                tokenType:   "Bearer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartCard authentication failed");
            return AuthenticationResult.Failure("SmartCard read error", ex);
        }
    }
}
```

**Contract:**

| Member | Requirement |
|---|---|
| `Name` | Must be unique across all registered strategies (case-insensitive) |
| `CanHandleAsync` | Return `false` when the prerequisite infrastructure is unavailable |
| `AuthenticateAsync` | Return `AuthenticationResult.Success(…)` or `AuthenticationResult.Failure(…)` — never throw |

---

## 3. Register with the builder

```csharp
builder.Services
    .AddAuthentication()
    .AddCustomStrategy<SmartCardStrategy>()
    .AddJwtTokenIssuance(o => { … });
```

`AddCustomStrategy<T>()` registers `T` as a transient `IAuthenticationStrategy`.

**With named options:**

```csharp
builder.Services
    .Configure<SmartCardOptions>("SmartCard", o =>
    {
        o.ReaderName = "Identiv SCR3310";
    });

builder.Services
    .AddAuthentication()
    .AddCustomStrategy<SmartCardStrategy>();
```

---

## 4. Use it

```csharp
var result = await tokenService.AuthenticateAsync("SmartCard");
```

`ITokenIssuanceService` wraps your strategy result in a JWT automatically — no extra plumbing.

---

## Returning additional claims

Include strategy-specific claims in the result so they appear in the issued JWT:

```csharp
return AuthenticationResult.Success(
    accessToken: certificate.Thumbprint,
    subject:     certificate.SubjectName.Name,
    claims: new Dictionary<string, string>
    {
        ["cert_thumbprint"] = certificate.Thumbprint,
        ["cert_issuer"]     = certificate.Issuer,
    });
```

The `TokenIssuanceService` maps `AuthenticationResult.Claims` into the JWT as custom claims.

---

## Health check integration

`CanHandleAsync` is called by the built-in health check. A strategy that returns `false` marks the check as **degraded**:

```csharp
builder.Services
    .AddAuthentication()
    .AddCustomStrategy<SmartCardStrategy>()
    .AddHealthCheck();    // will call SmartCardStrategy.CanHandleAsync periodically
```

<div class="bd-callout bd-callout-tip">
<strong>Tip:</strong> Make <code>CanHandleAsync</code> lightweight \u2014 it is polled by health checks. Reserve expensive
connectivity tests for <code>AuthenticateAsync</code>.
</div>
