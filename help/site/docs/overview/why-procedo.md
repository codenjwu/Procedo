---
title: Why Procedo
description: Understand where Procedo fits and why you might choose it over ad hoc scripting.
sidebar_position: 2
---

Procedo gives you a structured way to define and run operational workflows without hard-coding every execution path into an application.

It is a strong fit when you want:

- YAML-authored workflows that are easy to review
- step dependencies and controlled execution order
- reusable runtime behavior in a .NET host
- persistence and resume support for operator-driven flows
- extensibility through plugins and custom steps

## Good Fits

- local automation
- deployment and promotion workflows
- operator approval flows
- data preparation or packaging pipelines
- embedded workflow execution inside an application

## When It Helps More Than Scripts

Compared to a growing collection of shell scripts or custom orchestration code, Procedo gives you a clearer model for:

- dependencies
- runtime state
- validation
- observability
- resuming interrupted or waiting work

## Why YAML Matters Here

YAML is useful when you want the workflow definition to be:

- easy to review in pull requests
- separate from the host application's compiled logic
- editable without rewriting the runtime
- understandable by more than one engineer on the team

## Tradeoffs

Procedo is a strong fit for defined workflow execution, but it is not trying to be everything.

Current practical limits include:

- single-node runtime behavior
- file-backed persistence
- constrained template composition compared with a fully general graph-merging system

Those tradeoffs are often acceptable for local automation, embedded orchestration, and operator-friendly workflow execution.

## Next Steps

- [Core Concepts](./core-concepts.md)
- [Run Your First Workflow](../get-started/run-your-first-workflow.md)
