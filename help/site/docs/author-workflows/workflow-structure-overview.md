---
title: Workflow Structure Overview
description: See how stages, jobs, and steps fit together in a Procedo workflow.
sidebar_position: 1
---

Procedo workflow YAML is organized as a hierarchy. This structure helps workflows stay readable even when they grow into larger operational flows.

## The Hierarchy

Procedo organizes workflow definitions in this order:

1. workflow
2. stages
3. jobs
4. steps

## Example Structure

```yaml
name: example
version: 1

stages:
- stage: build
  jobs:
  - job: compile
    steps:
    - step: announce
      type: system.echo
      with:
        message: "Building"
```

## How To Read It

- The workflow contains one or more stages.
- Each stage contains one or more jobs.
- Each job contains one or more steps.
- Steps do the actual work.

## Why This Matters

The hierarchy is not just for readability. It also shapes how users think about execution:

- stages provide larger phases
- jobs group related work
- steps represent actual executable actions

As you move into more advanced examples, this structure also becomes the anchor point for:

- dependencies
- outputs
- runtime conditions
- persistence and resume behavior

## How This Scales

The same structure is used across the repository, from the smallest hello example to more complex scenario packs.

That means you do not need a different authoring model as your workflows become more sophisticated.

## Related Content

- [Steps](./steps.md)
- [Parameters](./parameters.md)
- [Outputs](./outputs.md)
