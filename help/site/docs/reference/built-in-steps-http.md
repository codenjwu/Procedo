---
title: "Built-in Steps: HTTP"
description: Use the built-in HTTP step for controlled outbound requests and understand the current security policy hooks.
sidebar_position: 23
---

`system.http` provides outbound HTTP calls inside a workflow.

## Current Input Shape

The current implementation supports inputs such as:

- `url`
- `method`
- `timeout_ms`
- `allow_non_success`
- `headers`
- `body`
- `content_type`

## Current Outputs

The step returns values such as:

- `status_code`
- `reason_phrase`
- `is_success`
- `body`
- `headers`

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/33_system_http_demo.yaml
```

The validated output includes:

```text
status=200
```

## Security Behavior

The current HTTP step is guarded by `SystemPluginSecurityOptions`:

- `AllowHttpRequests`
- `AllowedHttpHosts`

If HTTP is disabled, the step fails with a policy error. If allowed hosts are configured, only those hosts are permitted.

## Practical Guidance

- set `timeout_ms` explicitly for external calls
- use host allowlists when the runtime should only call approved destinations
- use `allow_non_success` only when non-2xx responses are part of expected control flow

## Related Content

- [Built-in Steps: Process and Security](./built-in-steps-process-and-security.md)
- [Built-in Steps: Secure Runtime](./built-in-steps-secure-runtime.md)
