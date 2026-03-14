---
title: YAML `name` and `version`
description: Understand the two top-level identity fields used by every Procedo workflow.
sidebar_position: 3
---

Every Procedo workflow should declare a `name` and a `version`.

## Example

```yaml
name: hello_echo
version: 1
```

## `name`

`name` identifies the workflow in logs, runtime output, and persisted run state.

Good names are:

- stable
- readable
- specific to the workflow's purpose

## `version`

`version` identifies the workflow document version.

Current examples in the repository consistently use:

```yaml
version: 1
```

## Practical Guidance

- keep `name` consistent across edits to the same workflow
- increase or track `version` when you need a clear document evolution signal
- avoid vague names such as `test` or `workflow1` in shared workflows

## Related Content

- [YAML Workflow Schema Overview](./yaml-workflow-schema-overview.md)
- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
