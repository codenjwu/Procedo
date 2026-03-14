---
title: "Built-in Steps: Process and Security"
description: Run external processes carefully and understand the current system plugin security controls.
sidebar_position: 23
---

`system.process_run` lets a workflow invoke an external executable, but this is also one of the more security-sensitive built-in capabilities.

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/40_system_process_demo.yaml
```

The current validated output includes:

```text
dotnet-version=10.0.200
```

## What The Example Uses

```yaml
- step: dotnet_version
  type: system.process_run
  with:
    file_name: dotnet
    arguments:
    - --version
    timeout_ms: 10000
```

## Current Security Controls

The current `SystemPluginSecurityOptions` model includes:

- `AllowHttpRequests`
- `AllowFileSystemAccess`
- `AllowProcessExecution`
- `AllowUnsafeExecutables`
- `AllowedPathRoots`
- `AllowedHttpHosts`
- `AllowedExecutables`

## Practical Guidance

- keep process execution disabled in restricted hosts unless it is truly needed
- prefer allowlists for executables in controlled environments
- use timeouts for external process calls
- treat process steps as higher-risk than pure data or utility steps

## Related Content

- [Built-in Steps: File and Directory](./built-in-steps-file-and-directory.md)
- [Built-in Steps Overview](./built-in-steps-overview.md)
