---
layout: default
title: Kerberos / Negotiate
description: Windows-integrated authentication via SSPI on Windows and GSSAPI on Linux (requires krb5.conf).
permalink: /strategies/kerberos/
---

## Overview

Acquires a Kerberos / Negotiate service ticket using `System.Net.Security.NegotiateAuthentication` (.NET 7+)
and returns a Base-64 encoded token suitable for the `Authorization: Negotiate` HTTP header.

---

## Platform requirements

| Platform | Requirement |
|---|---|
| Windows | Domain-joined machine or explicit credential |
| Linux / macOS | MIT Kerberos (`krb5`) and GSSAPI installed; valid `krb5.conf`; active TGT |

On platforms where Kerberos is unavailable, `CanHandleAsync()` returns `false` and
`AuthenticateAsync()` returns a failure result without throwing.

---

## Registration

### Windows SSO (current process identity)

```csharp
services.AddAuthentication()
    .AddKerberos(o =>
    {
        o.ServicePrincipalName = "HTTP/myservice.contoso.com";
    });
```

### Explicit credential (delegation)

```csharp
services.AddAuthentication()
    .AddKerberos(o =>
    {
        o.ServicePrincipalName = "HTTP/myservice.contoso.com";
        o.Package              = "Kerberos"; // or "Negotiate" to allow NTLM fallback
        o.Credential = new NetworkCredentialOptions
        {
            UserName = "svc-account",
            Password = configuration["Kerberos:Password"]!,
            Domain   = "CONTOSO"
        };
    });
```

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `ServicePrincipalName` | `string` | ✅ | — | SPN of the target service, e.g. `HTTP/host.domain` |
| `Package` | `string` | | `"Kerberos"` | SSPI/GSSAPI package — use `"Negotiate"` to permit NTLM fallback |
| `Credential` | `NetworkCredentialOptions?` | | `null` | Explicit credential; `null` = current process identity |

### `NetworkCredentialOptions`

| Property | Type | Required | Description |
|---|---|---|---|
| `UserName` | `string` | ✅ | Service account username |
| `Password` | `string` | ✅ | Service account password |
| `Domain` | `string?` | | Active Directory domain (e.g. `"CONTOSO"`) |

---

## How it works

1. `NegotiateAuthentication` is initialised with the configured SPN and optional credential.
2. `GetOutgoingBlob(ReadOnlySpan<byte>.Empty)` is called to produce the initial GSSAPI/SSPI token.
3. The token bytes are Base-64 encoded and returned as `AuthenticationResult.AccessToken`.
4. The caller places the value in the `Authorization: Negotiate <token>` header.

For multi-round Kerberos exchanges (uncommon in stateless HTTP), implement a custom strategy
that loops `GetOutgoingBlob` until `NegotiateAuthenticationStatusCode.Completed`.

---

## Subject claim

The `Subject` is set to `ServicePrincipalName`.

---

## Strategy name

```
"Kerberos"
```
