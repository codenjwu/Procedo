---
id: known-limitations
title: Known Limitations
sidebar_label: Known Limitations
description: Understand the current deliberate scope boundaries for Procedo, including single-node execution, file-backed persistence, template limits, and trusted-host security assumptions.
---

# Known limitations

Procedo Phase 1 is intentionally optimized for reliable single-node execution rather than full distributed orchestration. That focus keeps the runtime understandable and operator-friendly, but it also means some boundaries are deliberate rather than accidental.

## Runtime scope

Current runtime assumptions:

- single-node execution only
- no queue-backed scheduling or distributed worker coordination
- no built-in multi-tenant isolation for untrusted workflows

If you need clustered orchestration, horizontal worker leasing, or platform-grade tenant isolation, that is outside the current release scope.

## Persistence model

Current persistence assumptions:

- persistence is local and file-backed
- resume semantics are designed for local host recovery
- long-term state migration is intentionally lightweight

Persistence is a strong fit for local automation and resumable operator workflows. It is not yet a distributed durability story.

## Templates and DSL

Current authoring limits:

- templates support one base template rather than arbitrary graph composition
- template-time `${{ each }}` currently supports arrays rather than full object iteration
- parameter schemas cover practical validation, not full JSON Schema expressiveness
- step outputs still use `${steps.<stepId>.outputs.<key>}` rather than `vars.*`

These limits are important when you design reusable workflow libraries. Keep templates focused and predictable rather than trying to turn them into a full meta-programming system.

## Security model

Current security assumptions:

- `system.*` steps are designed for trusted-host usage
- file, HTTP, and process guardrails exist, but they are not a complete sandbox
- secret-management integration is not yet a built-in end-to-end feature

If you need to run untrusted workflows, you should assume extra host isolation and policy enforcement are required outside Procedo itself.

## Observability

Current observability assumptions:

- structured execution events are available
- console and JSONL sinks are the primary reference path
- a full metrics/tracing backend story is not yet built into Phase 1

## Packaging

Current package guidance:

- the intended public package surface is intentionally small
- some internal implementation projects exist in the repo but are not meant to be the main public onboarding path

For most users, start with `Procedo.Hosting` and only drop lower if you need tighter control.

## Related content

- [Support Matrix](./support-matrix)
- [Roadmap](./roadmap)
- [Security Runtime Guidance](../reference/built-in-steps-secure-runtime)
- [Embedding Procedo](../use-in-dotnet/embedding-procedo)
