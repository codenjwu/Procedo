---
id: phase-1-release-notes
title: Phase 1 Release Notes
sidebar_label: Phase 1 Release Notes
description: Product-facing summary of the Procedo 0.1.0 Phase 1 release, including highlights, compatibility guidance, and upgrade notes.
---

# Phase 1 release notes

Procedo Phase 1 is the first polished single-node release line for YAML-defined workflows, embedding, persistence, observability, and template-based authoring.

## Version

- Version: `0.1.0`
- Release date: `2026-03-12`

## Highlights

- YAML workflows built from `stages -> jobs -> steps`
- dependency-aware scheduling with outputs and expression resolution
- plugin-based runtime with built-in `system.*` steps
- local persistence, waiting workflows, and resume-by-run-id flows
- structured execution events for console and JSONL sinks
- template-time branching and loop expansion for reusable workflow authoring
- richer parameter schema validation for safer reusable workflows

## What this release added

This release established the main Procedo operating model:

- run workflows locally from the CLI host
- embed Procedo into .NET applications
- extend the runtime with plugins and custom steps
- persist and resume waiting runs
- validate workflow shape and parameter input before execution

It also expanded the example catalog so the docs can point to real, runnable workflows rather than only synthetic fragments.

## Important operational improvements

Phase 1 also delivered several operator-facing improvements:

- run inspection and cleanup workflows for persisted runs
- source attribution improvements for template-defined failures
- additive event contract improvements
- clearer package guidance around the intended public surface

## Compatibility notes

The current compatibility posture is intentionally conservative:

- workflow DSL changes are additive in the Phase 1 line
- existing required workflow fields remain stable
- event schema changes are additive and preserve `SchemaVersion = 1`
- runtime flag growth is additive rather than destructive

## Upgrade notes

Two points matter most for existing users:

- step outputs should be referenced as `${steps.<stepId>.outputs.<key>}`
- `vars.*` is reserved for workflow variables rather than step outputs

If you use persistence and resume, Phase 1 also makes parameter and workflow-variable behavior more consistent between fresh execution and resumed execution paths.

## Validation status

The engineering release notes for Phase 1 recorded:

- unit tests: green
- integration tests: green
- contract tests: green

In this help-site side project, the examples referenced throughout the docs are also being revalidated through a snippet command suite before publication.

## Recommended reading after upgrading

- [Install and Setup](../get-started/install-and-setup)
- [Persistence](../run-and-operate/persistence)
- [Validation](../run-and-operate/validation)
- [Built-in Steps Overview](../reference/built-in-steps-overview)
- [Known Limitations](./known-limitations)
