---
layout: default
title: Username / Password
description: Verify a username and password using HTTP Basic Auth encoding. Useful for internal services, legacy systems, and test environments.
permalink: /strategies/username-password/
---

## Overview

The Username/Password strategy implements **HTTP Basic Authentication** as defined in
[RFC 7617](https://datatracker.ietf.org/doc/html/rfc7617). It encodes the configured
`Username:Password` pair as Base-64 and produces a `Basic <base64>` credential value.

This strategy does _not_ validate the credential against an external directory (LDAP, Active
Directory, a database). It validates the credential against the static values you configure at
registration time. This makes it suitable for:

- **Internal service-to-service calls** where a shared secret is acceptable
- **Test and development environments** where a real identity provider is not available
- **Legacy integrations** that expect Basic Auth and cannot be updated to OAuth 2.0

For scenarios where you need to verify credentials against an external directory, implement a
[Custom Strategy]({{ '/strategies/custom/' | relative_url }}) that calls your user store.

---

## Registration

```csharp
services.AddAuthentication()
    .AddUsernamePassword(o =>
    {
        o.Username = "alice";
        o.Password = configuration["BasicAuth:Password"]!;  // load from secrets manager
    });
```

To register multiple named credentials — for example, different credentials per environment or per calling service:

```csharp
.AddUsernamePassword("ServiceA", o =>
{
    o.Username = "svc-a";
    o.Password = configuration["ServiceA:Password"]!;
})
.AddUsernamePassword("ServiceB", o =>
{
    o.Username = "svc-b";
    o.Password = configuration["ServiceB:Password"]!;
})
```

---

## How it works

When `AuthenticateAsync()` is called:

1. The `Username` and `Password` strings are encoded as UTF-8 bytes (or the configured `Encoding`).
2. They are concatenated as `Username:Password`.
3. The result is Base-64 encoded, producing the Basic Auth credential.
4. An `AuthenticationResult.Success` is returned with the credential as the access token and `Username` as the subject.
5. The JWT issuance layer then wraps this in a signed JWT — the raw Basic credential is not exposed to callers.

The intermediate byte array is zeroed out after use (`Array.Clear`) to minimize the window during which the plain-text password is in memory.

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Username` | `string` | ✅ | — | Login identifier. Becomes the JWT `sub` claim. |
| `Password` | `string` | ✅ | — | Plain-text password. Kept only in memory; never written to disk or logs. |
| `Realm` | `string?` | | `null` | Optional realm as defined in RFC 7617. Included in the credential string if set. |
| `Encoding` | `Encoding` | | `UTF-8` | Character encoding applied before Base-64 conversion. Only change this for legacy systems that require a different encoding (e.g. Latin-1). |

---

## Security considerations

**Credentials in memory.** The `Password` is held in the options object for the lifetime of the
application. It is never written to logs, disk, or the token itself. The intermediate byte array
used during Base-64 encoding is zeroed out immediately after use.

**Secrets manager.** Always load the password via `IConfiguration` backed by a secrets manager
(Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, or `dotnet user-secrets` in development).
Never hardcode it in source.

**HTTPS is required.** Even though the raw password is never transmitted (the JWT is), the
registration endpoint where credentials are exchanged for tokens must be served over HTTPS.
A network observer who can intercept the `POST /token` request gets the JWT — which is almost
as bad as getting the password.

**Compare with OIDC.** If your user store supports OAuth 2.0 (Azure AD, Okta, Auth0), prefer the
[OIDC strategy]({{ '/strategies/oidc/' | relative_url }}) with Client Credentials instead. The
identity provider then manages the credential lifecycle, MFA, Conditional Access, and audit logs.

---

## `CanHandleAsync` behaviour

`CanHandleAsync()` returns `false` when either `Username` or `Password` is null or whitespace.
This causes the library to skip `AuthenticateAsync()` entirely and return a failure result with a
descriptive message — useful for catching misconfiguration early (e.g. in a health check).

---

## Strategy name

```
"UsernamePassword"   (or whatever explicit name you passed to .AddUsernamePassword("name", o => …))
```

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
