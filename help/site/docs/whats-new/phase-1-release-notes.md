---
id: phase-1-release-notes
title: Phase 1 Release Notes
sidebar_label: Phase 1 Release Notes
description: Product-facing summary of the Procedo 1.0.0-rc1 Phase 1 release, including highlights, compatibility guidance, and upgrade notes.
---

# Phase 1 release notes

Procedo Phase 1 RC is the first polished single-node release line for YAML-defined workflows, embedding, persistence, callback-driven resume, observability, and template-based authoring.

## Version

- Version: `1.0.0-rc1`
- Release date: `2026-03-20`

## Highlights

- YAML workflows built from `stages -> jobs -> steps`
- dependency-aware scheduling with outputs and expression resolution
- plugin-based runtime with built-in `system.*` steps
- local persistence, active wait queries, resume-by-run-id, and callback-driven resume
- structured execution events for console and JSONL sinks with clearer resumed-run replay semantics
- template-time branching, array-only loop expansion, and runtime `condition:` gating for reusable workflow authoring
- richer parameter schema validation for safer reusable workflows
- broader executable example catalog and richer embedding projects

## What this release added

This release established the main Procedo operating model:

- run workflows locally from the CLI host
- embed Procedo into .NET applications
- extend the runtime with plugins and custom steps
- persist and resume waiting runs
- query active waits and resume by wait identity from host code
- validate workflow shape and parameter input before execution

It also expanded the example catalog so the docs can point to real, runnable workflows rather than only synthetic fragments.

## Important operational improvements

Phase 1 also delivered several operator-facing improvements:

- run inspection and cleanup workflows for persisted runs
- source attribution improvements for template-defined failures
- additive event contract improvements
- clearer package guidance around the intended public surface
- parity hardening between persisted and non-persisted execution paths

## Compatibility notes

The current compatibility posture is intentionally conservative:

- workflow DSL changes are additive in the Phase 1 line
- existing required workflow fields remain stable
- event schema changes are additive and preserve `SchemaVersion = 1`
- runtime flag growth is additive rather than destructive
- custom store adoption remains additive through capability interfaces rather than a forced contract rewrite

## Upgrade notes

Two points matter most for existing users:

- step outputs should be referenced as `${steps.<stepId>.outputs.<key>}`
- `vars.*` is reserved for workflow variables rather than step outputs
- `${{ each }}` is intentionally array-only in this phase
- callback-driven resume relies on persisted workflow snapshots for safe workflow reconstruction

If you use persistence and resume, Phase 1 also makes parameter and workflow-variable behavior more consistent between fresh execution and resumed execution paths.

## Validation status

The engineering release notes for this RC recorded:

- unit tests: `250/250 passed`
- integration tests: `145/145 passed`
- contract tests: `57/57 passed` across `net6.0`, `net8.0`, and `net10.0`

The help-site examples and embedding projects were also revalidated as part of the example-governance and smoke-test program.

## Recommended reading after upgrading

- [Install and Setup](../get-started/install-and-setup)
- [Persistence](../run-and-operate/persistence)
- [Callback-Driven Resume](../use-in-dotnet/callback-driven-resume)
- [Validation](../run-and-operate/validation)
- [Built-in Steps Overview](../reference/built-in-steps-overview)
- [Known Limitations](./known-limitations)
