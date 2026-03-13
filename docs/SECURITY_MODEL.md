# Security Model

Procedo Phase 1 targets trusted single-node execution.

This means:

- workflows are expected to run on a machine controlled by the application owner/operator
- no distributed orchestration trust boundary is assumed
- no full sandbox/isolation guarantee is provided in Phase 1

## Phase 1 security stance

Procedo should be treated as a trusted local workflow engine with guardrails, not as a secure multi-tenant workflow sandbox.

For Phase 1, the security model focuses on:

- local execution safety
- reducing accidental damage
- documenting risky capabilities clearly
- providing policy hooks for hosts to restrict dangerous behavior

## Trusted vs untrusted workflows

### Trusted workflows

Expected Phase 1 primary mode.

Characteristics:

- authored by the application owner/team
- reviewed or controlled by the operator
- executed in a known environment

### Untrusted workflows

Not a primary Phase 1 target.

If workflows are user-supplied or externally sourced, you should assume risk and add your own controls around:

- allowed step types
- allowed executables
- allowed file system paths
- network egress policy
- secret exposure

## Risky built-in capabilities

The highest-risk built-in steps currently include:

- `system.process_run`
- `system.http`
- file and directory operations
- archive extract operations depending on source inputs

## Current guardrails

Procedo now exposes `SystemPluginSecurityOptions` for the built-in system plugin.

This policy can be applied from an embedding app:

```csharp
var registry = new PluginRegistry();
registry.AddSystemPlugin(new SystemPluginSecurityOptions
{
    AllowHttpRequests = true,
    AllowProcessExecution = false,
    AllowFileSystemAccess = true,
    AllowedPathRoots = { ".procedo/runs", ".procedo/artifacts" },
    AllowedHttpHosts = { "api.contoso.test", "localhost" },
    AllowedExecutables = { "dotnet", "git" }
});
```

### `system.process_run`

Current protections include:

- shell executables blocked by default
- explicit override required for unsafe executable usage
- optional process allowlist via `AllowedExecutables`
- optional working-directory restriction via `AllowedPathRoots`
- global `AllowProcessExecution` kill switch
- timeout support
- captured stdout/stderr/exit code

### File and directory operations

Current protections include:

- global `AllowFileSystemAccess` kill switch
- optional root restriction via `AllowedPathRoots`
- policy checks for file, directory, zip, CSV, and file-backed hash operations

Recommended Phase 1 operational policy:

- run Procedo under a dedicated application working directory
- keep all workflow-managed files under explicit allowed roots
- avoid giving hosts broad filesystem permissions unnecessarily
- document allowed path conventions in the embedding application

### HTTP execution

Current protections include:

- global `AllowHttpRequests` kill switch
- optional host allowlist via `AllowedHttpHosts`

Recommended Phase 1 operational policy:

- restrict network access at the host/environment level if needed
- avoid embedding secrets directly in workflow YAML
- prefer host-provided configuration or secure secret injection patterns

## Runtime configuration hooks

`Procedo.Runtime` supports the same policy through `procedo.runtime.json` and environment variables.

Config section:

```json
{
  "systemSecurity": {
    "allowHttpRequests": true,
    "allowFileSystemAccess": true,
    "allowProcessExecution": true,
    "allowUnsafeExecutables": false,
    "allowedPathRoots": [".procedo/runs", ".procedo/artifacts"],
    "allowedHttpHosts": ["api.contoso.test", "localhost"],
    "allowedExecutables": ["dotnet", "git"]
  }
}
```

Environment variables:

- `PROCEDO_SYSTEM_ALLOW_HTTP`
- `PROCEDO_SYSTEM_ALLOW_FILESYSTEM`
- `PROCEDO_SYSTEM_ALLOW_PROCESS`
- `PROCEDO_SYSTEM_ALLOW_UNSAFE_EXECUTABLES`
- `PROCEDO_SYSTEM_ALLOWED_PATH_ROOTS`
- `PROCEDO_SYSTEM_ALLOWED_HTTP_HOSTS`
- `PROCEDO_SYSTEM_ALLOWED_EXECUTABLES`

List variables use comma-separated values.

Startup validation currently normalizes path roots, rejects executable allowlist entries that are paths instead of simple file names, and fails fast when persistence or JSON event output conflicts with configured file-system restrictions.

## Secrets and sensitive data

Phase 1 recommendation:

- treat workflow YAML as non-secret by default
- do not store secrets directly in YAML where avoidable
- structured execution events now redact `payload` output branches and common sensitive key names such as `token`, `secret`, `password`, and `api_key`
- keep step outputs JSON-friendly but do not emit secret material unless necessary
- embedding applications should still treat persisted state files and custom logs as sensitive operational data when resume payloads are used

## Recommended Phase 1 operational controls

For a production single-node release, recommended controls are:

- trusted-workflow-only usage unless extra controls are added
- explicit registration of allowed plugins/steps
- `RegisterOrThrow(...)` during startup to avoid accidental overrides
- use dedicated application directories for workflow file I/O
- configure `SystemPluginSecurityOptions` instead of relying on permissive defaults
- restrict machine-level permissions of the hosting process
- review usage of `system.process_run` carefully
- treat external HTTP/file/process steps as privileged operations

## Future security enhancements

Candidates for post-Phase-1 work:

- configurable step allow/deny policy model
- per-step trust modes
- stronger secret masking/redaction support
- sandboxed execution modes
- richer runtime diagnostics for blocked operations

## What Procedo Phase 1 does not claim

Phase 1 does not claim:

- multi-tenant isolation
- hostile workflow containment
- full sandboxing
- enterprise-grade security policy enforcement out of the box

Those can come later, but they should not be implied for the initial release.


