---
layout: default
title: Username / Password
description: HTTP Basic-style credential verification. Useful for internal services and legacy systems.
permalink: /strategies/username-password/
---

## Overview

Implements **HTTP Basic Authentication** as defined in [RFC 7617](https://datatracker.ietf.org/doc/html/rfc7617).
Encodes `username:password` as Base-64 and surfaces it as a `Basic` Authorization header value.

---

## Registration

```csharp
services.AddAuthentication()
    .AddUsernamePassword(o =>
    {
        o.Username = "alice";
        o.Password = configuration["BasicAuth:Password"]!;
    });
```

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Username` | `string` | ✅ | — | Login identifier |
| `Password` | `string` | ✅ | — | Plain-text password (kept only in memory) |
| `Realm` | `string?` | | `null` | Optional realm per RFC 7617 |
| `Encoding` | `Encoding` | | `UTF-8` | Encoding used before Base-64 conversion |

---

## Security considerations

- The plain-text password is stored **only in memory** as part of the options object. It is never written to disk.
- The intermediate byte array produced during Base-64 encoding is immediately zeroed out after use (`Array.Clear`).
- Always store the password in a secrets manager (e.g. Azure Key Vault, AWS Secrets Manager, .NET User Secrets) and inject it via `IConfiguration` rather than hardcoding it.
- Basic Auth transmits credentials on every request. **Always use HTTPS.**

---

## Subject claim

The `Subject` on `AuthenticationResult` is set to `Username`.  
This becomes the JWT `sub` claim when `ITokenIssuanceService` is used.

---

## Can handle check

`CanHandleAsync()` returns `false` when either `Username` or `Password` is null or whitespace.

---

## Strategy name

```
"UsernamePassword"
```
