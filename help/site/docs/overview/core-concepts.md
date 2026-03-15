---
title: Core Concepts
description: Learn the main building blocks of a Procedo workflow and runtime.
sidebar_position: 3
---

Procedo workflows are built from a small set of concepts that combine into larger execution graphs.

## Workflow

A workflow is the full YAML-defined unit of execution.

It contains metadata, optional parameters and variables, and one or more stages.

## Stage

A stage groups related jobs into a larger phase of work.

## Job

A job groups steps that belong together operationally.

Jobs can also carry execution policy settings such as max parallelism or continue-on-error behavior.

## Step

A step is the smallest executable unit in a workflow.

Each step has:

- a `type`
- optional inputs under `with`
- optional runtime gating with `condition:`
- optional dependencies with `depends_on`

## Parameters

Parameters let callers provide runtime input values to a workflow.

## Outputs

Outputs let one step expose values for later steps to consume.

This is the main mechanism for passing data forward through the workflow.

## Persistence And Resume

Procedo can persist run state so waiting or interrupted workflows can continue later.

## Observability

Procedo can emit structured execution events to help you inspect what happened during a run.

## Plugin Registry

Step types are resolved through a plugin registry. The runtime can load built-in plugins and application-provided extensions.

## Validation

Before execution, Procedo validates workflow structure, dependencies, and step registration so many errors are caught before runtime.

## Learn By Doing

- [Create Your First Workflow](../get-started/create-your-first-workflow.md)
- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
