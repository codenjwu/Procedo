# Next Enhancements

This document captures the agreed next enhancement priorities for Procedo after the current single-node Phase 1 foundation.

It is intended to be the working roadmap for upcoming implementation sessions.

## Current Position

Procedo already has:

- single-node workflow execution
- YAML workflows with stages, jobs, and steps
- plugin-based step execution
- step outputs and expression resolution
- local persistence and resume
- generic wait / signal / continue support
- validation
- structured observability events
- DI integration and custom step registration modes
- workflow templates, parameters, and variables
- source attribution for template loader errors, validation issues, run-level template failures, and step-level template failures
- run inspection via `--show-run`
- persisted run cleanup via targeted and bulk delete commands

## Agreed Next Priorities

The next work should focus on production usability and operator experience, with any Phase 1 scope changes explicitly tracked.

### Priority 1

- completed

### Priority 2

- completed

### Deferred for later

- advanced template composition
- large DSL expansion
- distributed orchestration
- broader observability platform work
- Azure DevOps style platform features beyond the newly requested basic flow/control and expression set

## Phase 1 Scope Change

The requested Azure-Pipelines-style starter flow/control set is now implemented for Phase 1:

- `${{ if }}`
- `${{ elseif }}`
- `${{ else }}`
- `${{ each }}` for arrays
- expression/condition functions such as `eq`, `ne`, `and`, `or`, `not`, `contains`, `startsWith`, `endsWith`, `in`, and `format`
- runtime step-level `condition:`

Future sessions should treat broader DSL expansion beyond that set as deferred follow-up work.

## Priority 1 Details

### 1. Run inspection / operator diagnostics

Status: Done

Goal:
- let operators inspect persisted runs without opening raw state files

Why it matters:
- Procedo now has persistence, wait/resume, and runtime source attribution
- operators need a simple way to understand current run state quickly

Suggested CLI:
- `--show-run <runId>`
- optional later: `--show-run <runId> --json`

Suggested output:
- run id
- workflow name
- current run status
- source path when available
- created / updated timestamps
- waiting step and reason when waiting
- failed step summary when failed
- completed / pending / waiting / failed step counts
- per-step status table
- optional compact outputs summary

Suggested tests:
- inspect completed run
- inspect waiting run
- inspect failed run
- inspect unknown run id
- inspect output formatting / json mode if added

Docs impact:
- update `docs/RUNBOOK.md`
- update `docs/PERSISTENCE.md`
- add examples to runtime usage docs

Release relevance:
- very high
- likely the most useful next operator feature

### 2. Bulk cleanup / retention commands

Status: Done

Goal:
- manage persisted run-state growth safely

Why it matters:
- single-node persisted state will accumulate over time
- manual one-by-one deletion is not enough for production use

Delivered CLI:
- `--delete-completed`
- `--delete-failed`
- `--delete-all-older-than <timespan>`
- `--delete-waiting-older-than <timespan>`

Deferred follow-up:
- optional dry-run mode later

Suggested behavior:
- print number of deleted runs
- avoid touching active/waiting runs unless explicitly requested
- validate timespan format clearly

Suggested tests:
- delete only completed
- delete only failed
- delete waiting older than threshold
- keep recent runs
- reject invalid combinations

Docs impact:
- update `docs/RUNBOOK.md`
- update `docs/PERSISTENCE.md`
- include retention guidance for `.procedo/runs`

Release relevance:
- high
- important for operating Procedo continuously on one machine

### 3. Per-step source attribution

Status: Done

Goal:
- carry template source information down to step-level runtime failure diagnostics

Why it matters:
- run-level attribution is now available
- step-level attribution completes the template debugging story

Delivered scope:
- `StepFailed` events now include `SourcePath`
- step-failure logs include source path when available
- additive, backwards-compatible event contract update

Completed tests:
- failing template-defined step emits template source path
- event contract remains stable with additive field

Docs impact:
- update `docs/TEMPLATES.md`
- update `docs/OBSERVABILITY.md`

Release relevance:
- high for template-based users
- medium overall, but very good next follow-up after current attribution work

## Priority 2 Details

### 4. Richer parameter schema validation

Status: Done

Goal:
- make parameter contracts safer and more expressive

Suggested areas:
- enum / allowed values
- numeric min / max
- string length / pattern
- array item typing
- object required fields

Suggested tests:
- valid and invalid typed values
- clearer validation messages
- CLI/config supplied values against schema

Release relevance:
- medium-high
- improves template and workflow correctness

### 5. Better operator docs / runbooks

Status: Done

Goal:
- make Procedo easier to run and troubleshoot without tribal knowledge

Suggested doc additions:
- inspect run workflow
- wait/resume operational flow
- cleanup / retention procedures
- common runtime failure patterns
- template debugging guidance

Release relevance:
- high
- low implementation cost, strong usability gain

### 6. Package / release polish

Status: Done

Goal:
- make the public release surface clear and professional

Suggested work:
- final package list review
- NuGet README alignment
- support matrix
- known limitations summary
- release note polish
- public/stable vs evolving contract summary

Release relevance:
- high for public release
- lower engineering risk, higher product polish

## Recommended Implementation Order

This wave is complete. Use future sessions to pick from deferred items or new roadmap entries.

## Out of Scope For This Next Wave

These are intentionally deferred:

- distributed execution and agent orchestration
- advanced matrix/loop DSL features
- fragment-based template composition
- hosted management UI
- artifact platform features
- approvals product layer / enterprise environment system

## Suggested Session Checklist

Use this sequence in future implementation sessions:

1. pick one planned item from Priority 1 or Priority 2
2. add or update docs first if the scope is non-trivial
3. implement the feature
4. add targeted unit/integration coverage
5. update changelog and relevant docs
6. mark status in this file if the item is completed

## Status Legend

- Planned
- In progress
- Done
- Deferred


