---
title: "Built-in Steps: Core Utilities"
description: Learn the basic built-in steps for emitting values, generating ids, composing strings, and pausing execution.
sidebar_position: 21
---

The core utility steps are the simplest `system.*` building blocks.

They are useful for:

- debug output
- timestamps and ids
- simple string composition
- short pauses during demo or sequencing flows

## Representative Steps

- `system.echo`
- `system.now`
- `system.guid`
- `system.concat`
- `system.sleep`

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/31_system_toolbox_demo.yaml
```

This example exercises:

- `system.now`
- `system.guid`
- `system.concat`
- `system.sleep`
- `system.echo`

## Expected Behavior

The current validated output includes a final line shaped like:

```text
timestamp=<utc timestamp> | guid=<generated guid>
```

The exact timestamp and guid values change each run.

## When To Use These Steps

- `system.echo` for messages and lightweight reporting
- `system.now` for time-stamping
- `system.guid` for unique ids
- `system.concat` for constructing readable output values
- `system.sleep` for short waits inside controlled scenarios

## Related Content

- [Built-in Steps Overview](./built-in-steps-overview.md)
- [Built-in Steps: Wait and Resume](./built-in-steps-wait-and-resume.md)
