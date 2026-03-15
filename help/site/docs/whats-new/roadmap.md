---
id: roadmap
title: Roadmap
sidebar_label: Roadmap
description: High-level view of where Procedo is likely to evolve next, based on the repo’s current roadmap and post-Phase 1 priorities.
---

# Roadmap

This roadmap page is a user-facing summary of the current post-Phase-1 direction. It is not a commitment to exact delivery dates, but it does show the kinds of improvements the project has already identified as high value.

## What Phase 1 established

Procedo already has a strong single-node foundation:

- YAML workflows with stages, jobs, and steps
- plugin-based execution
- outputs and expression resolution
- persistence and resume
- wait/signal flows
- validation
- observability
- DI integration and custom step registration
- templates, parameters, and workflow variables

## Recently completed priorities

The current roadmap notes that these operator-focused investments are already done:

- run inspection and diagnostics improvements
- persisted-run cleanup and retention commands
- richer parameter schema validation
- better operator docs and runbooks
- package and release-surface polish

## Likely future areas

The clearest future themes are:

- production usability improvements
- stronger operator ergonomics
- more polished docs and release communication
- carefully scoped authoring/runtime improvements without breaking the current mental model

## Intentionally deferred areas

The current roadmap is explicit about what is not the immediate focus:

- distributed execution and agent orchestration
- large DSL expansion
- fragment-based template composition
- hosted management UI
- artifact-platform features
- broad enterprise approvals/platform features

That is helpful for users because it sets realistic expectations. Procedo is growing from a stable single-node workflow engine outward rather than trying to solve every orchestration problem at once.

## How to read the roadmap

As the help site matures, this page should stay concise and user-oriented:

- what changed recently
- what kinds of improvements are most likely next
- what is intentionally out of scope

Detailed implementation planning can stay in engineering docs, while this page remains the quick product-facing summary.

## Related content

- [Release Notes Index](./release-notes-index)
- [Known Limitations](./known-limitations)
- [Phase 1 Release Notes](./phase-1-release-notes)
