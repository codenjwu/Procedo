---
title: YAML Workflow Schema Overview
description: See the high-level shape of a Procedo workflow document.
sidebar_position: 2
---

This page is the compact reference landing page for Procedo workflow YAML.

## Core Shape

```yaml
name: my_workflow
version: 1

parameters:
  environment: dev

stages:
- stage: main
  jobs:
  - job: run
steps:
    - step: say_hello
      type: system.echo
      with:
        message: "Hello"
```

## Main Sections

- `name`
- `version`
- `parameters`
- `variables`
- `stages`
- `jobs`
- `steps`
- `with`
- `condition`
- `depends_on`

## Current Workflow Model

The current workflow model in the repository includes these top-level concepts:

- `Name`
- `Version`
- `Template`
- `MaxParallelism`
- `ContinueOnError`
- parameter definitions and values
- variables
- stages

## Parameter Definition Capabilities

The current parameter definition model supports fields such as:

- `type`
- `required`
- `default`
- `description`
- `allowed_values`
- `min`
- `max`
- `min_length`
- `max_length`
- `pattern`
- `item_type`
- `required_properties`

## Job-Level Fields

Jobs currently support:

- `job`
- `max_parallelism`
- `continue_on_error`
- `steps`

## Step-Level Fields

Steps currently support:

- `step`
- `type`
- `condition`
- `with`
- `depends_on`
- `timeout_ms`
- `retries`
- `continue_on_error`

## How To Use This Page

Use this page as a compact entry point. Then move to focused pages when you need more detail on a specific area such as parameters, outputs, or conditions.

## Related Content

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Parameters](../author-workflows/parameters.md)
