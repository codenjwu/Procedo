---
title: YAML `stages`, `jobs`, and `steps`
description: Learn the core hierarchical execution structure used by Procedo workflows.
sidebar_position: 5
---

Procedo organizes executable workflow content into `stages`, `jobs`, and `steps`.

## Example

```yaml
stages:
- stage: build
  jobs:
  - job: pipeline
    steps:
    - step: download
      type: system.echo
      with:
        message: "download"
```

## `stages`

Each stage groups a larger phase of work.

Current model field:

- `stage`
- `jobs`

## `jobs`

Each job groups related steps and can carry execution-policy settings.

Current model field set:

- `job`
- `max_parallelism`
- `continue_on_error`
- `steps`

## `steps`

Each step is a single executable unit.

Current step field set includes:

- `step`
- `type`
- `condition`
- `with`
- `depends_on`
- `timeout_ms`
- `retries`
- `continue_on_error`

## Good Modeling Guidance

- use stages for broad phases
- use jobs for operational grouping
- use steps for the smallest clear unit of work

## Related Content

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Steps](../author-workflows/steps.md)
