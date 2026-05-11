---
layout: default
title: Strategies
description: Choose one or more authentication mechanisms and register them side-by-side. Every strategy is fully independent.
permalink: /strategies/
---

## Overview

Each strategy implements `IAuthenticationStrategy` and lives in its own vertical-slice folder inside the core package. Strategies are **independent by design** — you can add, remove, or replace one without touching any other.
exactly two files: the **options class** and the **strategy class**.  
Slices are fully independent — adding, modifying, or removing one has no effect on any other.

| Strategy | Name | Mechanism |
|---|---|---|
| [OIDC](oidc) | `"OIDC"` | OAuth 2.0 Client Credentials or ROPC via MSAL.NET |
| [Username / Password](username-password) | `"UsernamePassword"` | HTTP Basic Auth (RFC 7617) |
| [Kerberos](kerberos) | `"Kerberos"` | Kerberos / Negotiate via SSPI / GSSAPI |
| [API Key](api-key) | `"ApiKey"` | Custom header, Bearer token, or query parameter |
