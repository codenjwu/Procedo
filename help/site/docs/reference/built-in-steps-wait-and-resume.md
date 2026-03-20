---
title: "Built-in Steps: Wait and Resume"
description: Use built-in waiting steps for approval-style and signal-driven workflow flows, including persisted and callback-style resume.
sidebar_position: 24
---

Procedo includes built-in steps for workflows that need to pause and continue later.

Current wait-oriented step types include:

- `system.wait_signal`
- `system.wait_until`
- `system.wait_file`

## Wait For A Signal

The simplest waiting example is:

```yaml
- step: wait_here
  type: system.wait_signal
  with:
    signal_type: continue
    key: approval-demo
    reason: "Waiting for external continue signal"
```

For template-authored or more explicit cases, the wait key may also appear as `wait_key`.

That key is what host-side callback-driven resume typically matches on.

## Validated Waiting Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/help-wait-runs
```

## What To Expect

The current runtime behavior shows:

- the workflow enters a waiting state
- the runtime prints the waiting reason
- a run id is emitted
- the state directory is printed
- the persisted wait contains wait identity metadata such as wait type, wait key, and expected signal type

The validated shell exit code for this command is `2`, which is how the runtime signals a waiting result.

## When To Use Wait Steps

- approval workflows
- external coordination points
- file-based readiness checks
- operator-driven continuation flows

## Resume Paths

Procedo currently supports two main resume paths:

- CLI/runtime resume by `runId`
- host-driven resume by wait identity

Use host-driven resume when your application receives a callback or approval and needs to locate the correct waiting run first.

## Related Content

- [Persistence](../run-and-operate/persistence.md)
- [CLI Overview](./cli-overview.md)
- [Callback-Driven Resume](../use-in-dotnet/callback-driven-resume.md)
