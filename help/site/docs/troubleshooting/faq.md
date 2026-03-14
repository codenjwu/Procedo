---
title: FAQ
description: Quick answers to common Procedo questions.
sidebar_position: 2
---

## When should I use Procedo instead of scripts?

Use Procedo when you want workflows with structure, validation, persistence, runtime gating, and inspectable execution behavior.

## Where should I start learning?

Start with:

- [Install and Setup](../get-started/install-and-setup.md)
- [Run Your First Workflow](../get-started/run-your-first-workflow.md)
- [Create Your First Workflow](../get-started/create-your-first-workflow.md)

## Where do examples live?

Repository examples live under `examples/`.

## How will snippets stay trustworthy?

The help project is designed around using tested example files as the source of truth for published snippets.

## Can I use Procedo without writing a custom app first?

Yes. The CLI host in `src/Procedo.Runtime` is the easiest way to run workflow files directly.

## When should I use parameters instead of editing YAML per environment?

Use parameters when the workflow shape is the same and only the runtime values change.

## When should I use `condition:` instead of templates?

Use `condition:` when the step still belongs in the workflow but should sometimes be skipped at runtime. Use templates when the generated workflow structure itself should change before runtime.
