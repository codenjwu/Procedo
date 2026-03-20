# Test Strategy

This document defines how Procedo should verify engine behavior as the feature surface grows.

Tests are a core product value for Procedo. They protect the engine, the public package story, the DSL contract, and the example catalog.

## Goals

The Procedo test strategy should:

- prove engine correctness
- protect public API and DSL compatibility
- treat examples as executable product contracts
- verify that major features compose cleanly
- catch drift between docs, examples, and runtime behavior

## Verification layers

Procedo should continue to use multiple test layers, each with a clear job.

### 1. Unit tests

Purpose:

- verify isolated behavior quickly
- protect parser, validator, expressions, scheduler helpers, and persistence helpers

Best for:

- expression functions
- null handling
- parser edge cases
- store normalization
- scheduler policy units
- plugin registry behavior

### 2. Integration tests

Purpose:

- verify whole workflow behavior end to end
- prove feature composition
- assert run outcomes, step states, and event flows

Best for:

- retries
- timeouts
- persistence/resume
- callback-driven resume
- runtime conditions
- templates and conditional expansion
- observability event sequences
- artifact-producing workflows

### 3. Contract tests

Purpose:

- protect public guarantees across supported target frameworks

Best for:

- public package surface
- extensibility APIs
- execution event schema
- store capability compatibility
- active wait models

### 4. Example verification tests

Purpose:

- treat examples as executable documentation
- ensure the catalog stays trustworthy

Best for:

- parse/validate all examples
- run all supported success examples
- classify expected failures and waiting examples
- verify important examples with golden assertions

## Required test tracks

Procedo should invest in four explicit test tracks going forward.

### A. Example governance tests

These tests treat the example catalog as a product contract.

They should:

- parse every YAML example
- validate every YAML example
- classify each example by expected outcome
- verify every catalog-listed example has coverage
- fail if an example is renamed or removed without catalog/test updates

Recommended suite:

- `WorkflowExampleCatalogTests`

Recommended expectations:

- success examples execute cleanly
- validation-failure examples fail validation for the intended reason
- runtime-failure examples fail execution for the intended reason
- waiting examples enter the expected waiting state
- catalog verification should exercise the documented run path or a faithful automated equivalent for runnable examples

### B. Golden scenario tests

These are deeper end-to-end tests for important examples.

They should assert:

- run result status
- step state map
- waiting/skipped/completed distinctions
- key outputs
- key artifacts
- key events

Recommended targets:

- every major real-world scenario
- every advanced wait/resume scenario
- every major template/control-flow scenario

### C. Parity tests

These are especially important after the persisted execution hardening work.

They should run the same workflow in:

- non-persisted mode
- persisted mode
- persisted resumed mode when applicable

And compare meaningful outcomes for:

- retries
- timeouts
- `continue_on_error`
- `max_parallelism`
- outputs
- failure propagation
- skipped behavior

Recommended suite:

- `WorkflowParityIntegrationTests`

### D. Semantics matrix tests

These protect nuanced rules that are easy to regress.

They should cover:

- null semantics
- runtime `condition:` semantics
- expression-function behavior
- template branching behavior
- `${{ each }}` array-only behavior
- replayed resume event semantics
- callback-resume matching behavior
- workflow-drift protection

Recommended suite families:

- `WorkflowNullSemanticsIntegrationTests`
- `WorkflowControlFlowMatrixTests`
- `WorkflowResumeEventSemanticsTests`

## Example-first verification model

For every major new feature, Procedo should aim to ship:

- one focused example
- one composed example
- one real-world scenario
- one automated verification path

That verification path may be:

- a unit test
- an integration test
- a contract test
- an example-governance test

But it should exist at release time.

For examples, “done” should mean more than “file added”:

- the example is documented
- the example has a classified expected outcome
- the example has been executed with its documented command, or with a faithful automated harness equivalent
- the result matches the documented expectation

## Coverage expectations by feature area

### Workflow execution core

Required:

- retries
- timeouts
- cancellation
- dependency blocking
- `continue_on_error`
- `max_parallelism`
- skipped behavior

### Persistence and resume

Required:

- persisted happy path
- persisted failure/recovery
- repeated wait/resume cycles
- callback-driven resume
- concurrency-safe resume claims
- workflow snapshot/drift behavior
- replayed event semantics

### DSL and templates

Required:

- parse/validate simple workflows
- template inheritance
- parameter validation
- `${{ if }}` / `${{ elseif }}` / `${{ else }}`
- `${{ each }}`
- runtime `condition:`
- null handling across parse/merge/runtime

### Public API and package surface

Required:

- package smoke
- cross-target contract tests
- extensibility contract tests
- event schema contract tests
- store capability compatibility tests

### Examples

Required:

- all examples parse
- all examples validate as classified
- catalog examples remain runnable
- important examples have golden assertions

## Suggested new test suites

Recommended additions:

- `WorkflowExampleCatalogTests`
- `WorkflowParityIntegrationTests`
- `WorkflowNullSemanticsIntegrationTests`
- `WorkflowControlFlowMatrixTests`
- `WorkflowResumeEventSemanticsTests`
- `WorkflowScenarioGoldenTests`

## Test ownership rules

Use the smallest useful layer:

- parser or resolver nuance: unit test
- workflow behavior across steps/jobs/stages: integration test
- public guarantee across frameworks/packages: contract test
- user-facing example trustworthiness: example-governance test

Do not rely on only one layer when behavior is both user-facing and architecturally important.

## Example verification matrix

Each example should eventually be marked with:

- parse coverage
- validation coverage
- execution coverage
- golden coverage

Target model:

- every YAML example: parse + validation
- every documented success example: execution
- every advanced scenario example: golden coverage

## Release bar

Before a release candidate, Procedo should be able to say:

- full unit suite is green
- full integration suite is green
- full contract suite is green
- public pack smoke is green
- example governance suite is green
- all high-value scenario examples have golden coverage

## Milestones

### Milestone 1: Catalog governance

Build the example inventory and classification tests.

### Milestone 2: Parity and semantics

Add parity and null/control-flow/resume semantics suites.

### Milestone 3: Golden scenarios

Add golden tests for the most important real-world examples.

### Milestone 4: Ongoing release discipline

Make example verification part of the standard release sweep.

Implementation tracking for this strategy lives in:

- `docs/EXAMPLE_AND_TEST_BACKLOG.md`
