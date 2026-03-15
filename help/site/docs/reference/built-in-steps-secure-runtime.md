---
title: "Built-in Steps: Secure Runtime"
description: Lock down built-in system capabilities with the current system plugin security options.
sidebar_position: 27
---

The built-in `system.*` catalog can be run in a constrained mode through `SystemPluginSecurityOptions`.

## Current Security Options

The current security model includes:

- `AllowHttpRequests`
- `AllowFileSystemAccess`
- `AllowProcessExecution`
- `AllowUnsafeExecutables`
- `AllowedPathRoots`
- `AllowedHttpHosts`
- `AllowedExecutables`

## Validated Secure Runtime Example

```powershell
dotnet run --project examples/Procedo.Example.SecureRuntime
```

This example configures a locked-down host that:

- allows controlled filesystem access within an approved root
- blocks outbound HTTP
- blocks process execution

## What The Example Proves

The validated example runs one allowed workflow and one blocked workflow:

- the allowed file-writing workflow succeeds
- the blocked process-running workflow fails as expected

## Security Guardrails

The current implementation enforces:

- host allowlists for filesystem paths
- host allowlists for HTTP hosts
- executable allowlists when configured
- default blocking of shell-like executables such as `cmd`, `powershell`, `pwsh`, `bash`, and `sh` unless unsafe execution is explicitly allowed

## When To Use A Secure Runtime

- production hosts
- operator-facing workflow runtimes
- environments that should only permit narrow filesystem or process access
- scenarios where workflows come from shared or semi-trusted sources

## Related Content

- [Built-in Steps: HTTP](./built-in-steps-http.md)
- [Built-in Steps: Process and Security](./built-in-steps-process-and-security.md)
