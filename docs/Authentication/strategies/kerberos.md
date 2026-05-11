---
layout: default
library: authentication
title: Kerberos / Negotiate
description: Windows-integrated authentication via SSPI on Windows and GSSAPI on Linux. Ideal for domain-joined services and intranet applications.
permalink: /authentication/strategies/kerberos/
---

## Overview

Kerberos is a network authentication protocol designed for use within a trusted network domain (typically Active Directory). Instead of transmitting passwords, Kerberos uses **tickets** — time-limited cryptographic tokens issued by a Key Distribution Center (KDC, which is your domain controller). A client proves its identity by presenting a valid ticket for a specific service.

The Kerberos strategy uses `System.Net.Security.NegotiateAuthentication` (.NET 7+) via the OS’s SSPI layer on Windows or GSSAPI on Linux/macOS. It produces a Base-64 encoded Kerberos/Negotiate token suitable for the `Authorization: Negotiate <token>` HTTP header.

**Use this strategy when:**
- Your application runs inside a Windows Active Directory domain
- You need single sign-on (SSO) — users or services authenticate transparently with their domain credentials, no password prompt required
- You’re integrating with on-premises services (SQL Server with Windows Auth, SharePoint, legacy IIS apps) that require Negotiate/Kerberos

---

## Platform requirements

| Platform | Requirement |
|---|---|
| Windows | Domain-joined machine. The process runs as a domain identity (service account or the logged-in user). No additional packages needed. |
| Linux | MIT Kerberos (`krb5-user` or `krb5-workstation`) and GSSAPI (`libgssapi-krb5-2`) installed. A valid `/etc/krb5.conf` pointing at your KDC. An active Ticket Granting Ticket (TGT) — typically from `kinit` or a keytab. |
| macOS | Kerberos support via the system Heimdal or MIT Kerberos. Requires a valid `/etc/krb5.conf` and TGT. |
| Container | Possible but complex. The container needs access to the KDC (network line-of-sight), a valid keytab mounted as a secret, and periodic `kinit` refresh or a sidecar. Consider whether Kerberos is the right choice for containerised workloads. |

`CanHandleAsync()` returns `false` on platforms where `NegotiateAuthentication` is unavailable or
when the SPN is not configured, so the library degrades gracefully rather than throwing.

---

## Registration

### Windows SSO — current process identity (most common)

The simplest case: the process runs as a domain service account, and Kerberos uses that identity
to acquire a service ticket. No password configuration needed.

```csharp
services.AddAuthentication()
    .AddKerberos(o =>
    {
        // The Service Principal Name identifies which service you are authenticating to.
        // Format: HTTP/hostname or HTTP/hostname.domain.com
        o.ServicePrincipalName = "HTTP/myservice.contoso.com";
    });
```

### Explicit credential (delegation or cross-domain)

Use this when the process runs under a non-domain identity (e.g. a container or a local account)
and you need to authenticate with a specific domain service account:

```csharp
services.AddAuthentication()
    .AddKerberos(o =>
    {
        o.ServicePrincipalName = "HTTP/myservice.contoso.com";

        // Use "Negotiate" to allow NTLM fallback if Kerberos tickets cannot be obtained.
        // Prefer "Kerberos" in production to force Kerberos and prevent NTLM downgrade.
        o.Package = "Kerberos";

        o.Credential = new NetworkCredentialOptions
        {
            UserName = "svc-account",
            Password = configuration["Kerberos:Password"]!,
            Domain   = "CONTOSO"
        };
    });
```

---

## How it works

1. `NegotiateAuthentication` is initialized with the configured SPN and optional credential.
2. `GetOutgoingBlob(ReadOnlySpan<byte>.Empty)` is called to produce the initial GSSAPI/SSPI token — this is the Kerberos service ticket encrypted for the target SPN.
3. The token bytes are Base-64 encoded and returned as `AuthenticationResult.AccessToken`.
4. The caller (or the Primitives HTTP client handler) places this value in the `Authorization: Negotiate <token>` header.
5. The target service validates the ticket against its own keytab or via the KDC.

For multi-round Kerberos exchanges (uncommon in stateless HTTP but possible in some SSPI
scenarios), implement a [Custom Strategy]({{ '/authentication/strategies/custom/' | relative_url }}) that loops
`GetOutgoingBlob` until `NegotiateAuthenticationStatusCode.Completed`.

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `ServicePrincipalName` | `string` | ✅ | — | SPN of the target service, e.g. `HTTP/host.domain.com`. The KDC uses this to find the right service ticket. |
| `Package` | `string` | | `"Kerberos"` | SSPI/GSSAPI package. Use `"Negotiate"` to permit NTLM fallback; use `"Kerberos"` to enforce Kerberos only. |
| `Credential` | `NetworkCredentialOptions?` | | `null` | Explicit domain credential. `null` means use the current process identity — the recommended choice for domain-joined services. |

### `NetworkCredentialOptions`

| Property | Type | Required | Description |
|---|---|---|---|
| `UserName` | `string` | ✅ | Domain service account username (without domain prefix) |
| `Password` | `string` | ✅ | Service account password. Load from a secrets manager, never hardcode. |
| `Domain` | `string?` | | Active Directory domain name, e.g. `"CONTOSO"`. Optional if the username includes the domain. |

---

## Subject claim

The `Subject` on `AuthenticationResult` is set to `ServicePrincipalName`. This becomes the JWT `sub` claim.

---

## Strategy name

```
"Kerberos"   (or whatever explicit name you passed to .AddKerberos("name", o => …))
```

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