# Example And Test Backlog

## Purpose

This document turns the example strategy and test strategy into a concrete implementation backlog.

Examples and tests are a core product value for Procedo. This backlog treats them as first-class deliverables alongside engine code and packaging work.

It focuses on:

- expanding the example catalog deliberately
- treating examples as executable product contracts
- increasing complex scenario coverage
- increasing verification confidence for new and composed features

## Epic

- `PROC-EX-EPIC`
  Title: Example Catalog And Verification Expansion
  Goal: Make the Procedo example catalog broad, realistic, and trustworthy enough to serve as a product surface and regression safety net.
  Description: Add foundational examples, composed examples, real-world scenario packs, embedding examples, and example-first verification suites that prove the documented behavior of the engine and public package story.
  Acceptance Criteria:
  - major feature areas have focused and realistic examples
  - the example catalog is classified and discoverable
  - examples are covered by automated verification according to their importance
  - every newly added runnable example is executed with its documented command before the story is considered complete
  - advanced scenarios demonstrate real operator workflows rather than only micro-demos
  - release verification includes example governance and golden scenario coverage
  Dependencies:
  - current Phase 1 engine surface
  Risks or notes:
  - examples should not drift into toy-only content
  - tests should verify meaningful behavior, not just process exit codes

## Milestones

### Milestone 1: Catalog governance

Focus:

- example inventory
- example classification
- parse and validation coverage for the whole catalog

### Milestone 2: Foundation semantics pack

Focus:

- focused examples for nuanced behavior
- parity, null semantics, callback resume basics

### Milestone 3: Composition pack

Focus:

- feature composition examples
- composed verification suites

### Milestone 4: Real-world scenario packs

Focus:

- release, incident, ETL, audit, model-promotion, and maintenance scenarios
- golden scenario verification

### Milestone 5: Embedding maturity

Focus:

- richer example projects
- host-facing callback, parity, policy, and observability examples

## Ordered Stories

### `PROC-EX-001`

Title: Build example inventory and classification model

Goal:
- Establish a stable inventory of YAML examples and example projects with clear metadata.

Description:
- Define and document the classification model for examples in the catalog. Every example should have a stable classification such as category, expected outcome, required plugins, persistence requirement, resume requirement, artifact production, and intended audience.

Acceptance Criteria:
- a documented classification model exists
- the current example catalog is mapped to that model
- example naming and category rules are documented
- success, validation-failure, runtime-failure, waiting, and resume-required examples are distinguishable
- the inventory records whether an example must be executed directly, through a host project, or through automated tests

Dependencies:
- None

Risks or notes:
- this is foundational for automated catalog governance

### `PROC-EX-002`

Title: Add example governance test suite

Goal:
- Treat the example catalog as an executable product contract.

Description:
- Add a dedicated suite such as `WorkflowExampleCatalogTests` that parses and validates the example inventory, checks catalog coverage, and asserts expected classifications.

Acceptance Criteria:
- every YAML example is parsed automatically
- every YAML example is validated automatically
- every example listed in `examples/README.md` is covered by the inventory
- classified validation-failure examples fail for the intended reason
- classified waiting examples are recognized distinctly from immediate success examples
- classified runnable success examples are executed with their documented command or an equivalent automated harness path

Dependencies:
- `PROC-EX-001`

Risks or notes:
- this suite should stay fast and deterministic

### `PROC-EX-003`

Title: Add null semantics example pack

Goal:
- Make null behavior explicit and teachable for users.

Description:
- Add focused examples covering YAML `null`, `~`, empty string, literal `"null"`, template null overrides, and null-bearing step inputs.

Acceptance Criteria:
- catalog includes a focused null semantics example
- examples demonstrate `null`, `""`, and `"null"` distinctly
- examples demonstrate template and runtime input behavior
- docs reference the new examples from validation/templates material
- each new example is executed successfully with the documented command before the story closes

Dependencies:
- None

Risks or notes:
- this is a high-value teaching area because the semantics are subtle

### `PROC-EX-004`

Title: Add null semantics verification suite

Goal:
- Lock down the runtime behavior demonstrated by the new null examples.

Description:
- Add a suite such as `WorkflowNullSemanticsIntegrationTests` that verifies parse, template merge, runtime input resolution, persistence, and resume behavior for null-bearing workflows.

Acceptance Criteria:
- tests cover YAML parameter nulls
- tests cover template override nulls
- tests cover step input nulls
- tests cover persisted and resumed null round-trip behavior

Dependencies:
- `PROC-EX-003`

Risks or notes:
- should assert actual run behavior, not only parser output

### `PROC-EX-005`

Title: Add persisted parity example pack

Goal:
- Demonstrate that persisted and non-persisted execution honor the same runtime policies.

Description:
- Add focused examples covering retries, timeouts, `continue_on_error`, `max_parallelism`, wait/resume, and skipped behavior with both persisted and non-persisted execution paths in mind.

Acceptance Criteria:
- catalog includes parity-oriented examples
- examples are runnable in both persisted and non-persisted modes where applicable
- examples are referenced from persistence and readiness docs
- the documented run commands for the new examples are exercised successfully as part of implementation

Dependencies:
- None

Risks or notes:
- keep these examples small and diagnostic

### `PROC-EX-006`

Title: Add persisted parity verification suite

Goal:
- Prove that persisted and non-persisted runs behave equivalently for core workflow semantics.

Description:
- Add a dedicated suite such as `WorkflowParityIntegrationTests` that runs the same workflow in multiple execution modes and compares meaningful outcomes.

Acceptance Criteria:
- tests cover retries
- tests cover timeouts
- tests cover `continue_on_error`
- tests cover `max_parallelism`
- tests cover outputs and skipped behavior
- tests cover wait/resume parity where applicable

Dependencies:
- `PROC-EX-005`

Risks or notes:
- compare meaningful outputs and states, not only top-level success flags

### `PROC-EX-007`

Title: Add callback-driven resume basics example pack

Goal:
- Make callback-driven resume understandable without reading engine internals.

Description:
- Add focused examples showing active wait identity, callback-style resume lookup, repeated wait/resume cycles, and persisted workflow snapshot safety in a host-agnostic way.

Acceptance Criteria:
- catalog includes focused callback-resume examples
- examples demonstrate resume-by-identity concepts clearly
- examples stay generic and transport-agnostic
- the documented happy-path example commands are executed successfully during implementation

Dependencies:
- current callback-driven resume implementation

Risks or notes:
- avoid HTTP or webhook-specific framing

### `PROC-EX-008`

Title: Add callback-driven resume verification suite

Goal:
- Prove callback-driven resume behavior for real workflows.

Description:
- Add integration tests for active wait query behavior, resume-by-identity, repeated wait/resume cycles, and workflow drift protection using realistic example-style workflows.

Acceptance Criteria:
- tests cover successful resume-by-identity
- tests cover duplicate-match or stale-match behavior
- tests cover repeated wait/resume cycles
- tests cover workflow snapshot/drift safety

Dependencies:
- `PROC-EX-007`

Risks or notes:
- keep tests transport-agnostic and engine-level

### `PROC-EX-009`

Title: Add control-flow semantics matrix examples

Goal:
- Provide a clear example surface for template-time and runtime flow-control behavior.

Description:
- Add focused examples for `${{ if }}`, `${{ elseif }}`, `${{ else }}`, array-only `${{ each }}`, runtime `condition:`, and mixed control-flow with structured parameters.

Acceptance Criteria:
- control-flow examples cover both isolated and mixed usage
- `${{ each }}` array-only behavior is shown clearly
- object/dictionary iteration is either absent or explicitly shown as unsupported
- the documented run commands for the new control-flow examples are exercised successfully

Dependencies:
- None

Risks or notes:
- examples should stay focused and not collapse into one giant file

### `PROC-EX-010`

Title: Add control-flow matrix verification suite

Goal:
- Protect the composed semantics of branching, iteration, and runtime gating.

Description:
- Add a dedicated suite such as `WorkflowControlFlowMatrixTests` that verifies template expansion, runtime conditions, and mixed control-flow behaviors.

Acceptance Criteria:
- tests cover `${{ if }}` / `${{ elseif }}` / `${{ else }}`
- tests cover array-only `${{ each }}`
- tests cover runtime `condition:`
- tests cover mixed template-time plus runtime control flow

Dependencies:
- `PROC-EX-009`

Risks or notes:
- should verify both graph shape and run outcome

### `PROC-EX-011`

Title: Add composed example pack for templates, persistence, and observability

Goal:
- Demonstrate feature composition in medium-complexity workflows.

Description:
- Add examples that combine templates, null overrides, runtime conditions, persistence, resume, and observability in one coherent authoring style.

Acceptance Criteria:
- catalog includes medium-complexity composition examples
- examples demonstrate multiple advanced features without becoming unreadable
- examples have clear run commands and expected outcomes
- each new composition example is executed successfully with the documented command before the story closes

Dependencies:
- `PROC-EX-003`
- `PROC-EX-005`
- `PROC-EX-009`

Risks or notes:
- these should feel like real authoring patterns, not stitched-together feature matrices

### `PROC-EX-012`

Title: Add composition golden tests

Goal:
- Verify meaningful outcomes for medium-complexity example combinations.

Description:
- Add golden integration coverage for the new composition examples, including step states, outputs, events, and artifacts where relevant.

Acceptance Criteria:
- selected composition examples have golden assertions
- tests verify outputs, state transitions, and key events
- failures are diagnostic and not overly brittle

Dependencies:
- `PROC-EX-011`

Risks or notes:
- prefer a few strong golden tests over many shallow ones

### `PROC-EX-013`

Title: Add real-world release scenario pack

Goal:
- Expand release-oriented examples into a richer scenario family.

Description:
- Add release workflows covering canary, approval, degraded path, recovery, rollback packaging, and staged promotion.

Acceptance Criteria:
- at least one happy-path release example exists
- at least one degraded or recovery release example exists
- examples demonstrate realistic artifacts and operator flow
- the documented run paths for the new release examples are executed successfully

Dependencies:
- None

Risks or notes:
- keep the workflows single-node and engine-level

### `PROC-EX-014`

Title: Add incident and maintenance scenario pack

Goal:
- Expand operational examples beyond release orchestration.

Description:
- Add realistic incident triage, evidence collection, maintenance-window, and runbook-style workflows using current engine capabilities.

Acceptance Criteria:
- incident examples exist
- maintenance or runbook examples exist
- examples demonstrate branching, waiting, and artifact collection where relevant
- the documented run paths for the new incident and maintenance examples are executed successfully

Dependencies:
- None

Risks or notes:
- avoid scenarios that imply unsupported distributed orchestration

### `PROC-EX-015`

Title: Add data, audit, and model-promotion scenario pack

Goal:
- Cover additional high-value enterprise workflow families.

Description:
- Add realistic ETL, compliance/audit, and model-promotion scenarios with both happy-path and degraded-path coverage where practical.

Acceptance Criteria:
- ETL or data workflow examples exist
- compliance or audit workflow examples exist
- model-promotion workflow examples exist
- examples demonstrate complex but readable multi-step flows
- the documented run paths for the new scenario examples are executed successfully

Dependencies:
- None

Risks or notes:
- keep examples understandable even when the scenarios are larger

### `PROC-EX-016`

Title: Add scenario golden test suite

Goal:
- Treat high-value scenario examples as regression anchors.

Description:
- Add a suite such as `WorkflowScenarioGoldenTests` that exercises selected real-world scenarios and verifies run results, step states, artifacts, and key events.

Acceptance Criteria:
- major scenario families have golden test coverage
- tests cover success and at least some degraded or recovery paths
- artifact-producing scenarios verify actual outputs

Dependencies:
- `PROC-EX-013`
- `PROC-EX-014`
- `PROC-EX-015`

Risks or notes:
- golden tests should assert important outputs only, not every incidental file detail

### `PROC-EX-017`

Title: Add embedding example project pack

Goal:
- Expand the .NET host-facing example story.

Description:
- Add richer example projects for callback-driven resume, advanced observability, policy-driven hosting, enterprise scenario running, and persisted-vs-non-persisted comparison.

Acceptance Criteria:
- new host example projects exist
- example projects align with the five-package public story
- example projects are discoverable from the catalog
- each new example project is executed successfully with its documented entry command

Dependencies:
- None

Risks or notes:
- keep project examples focused on public packages and supported host APIs

### `PROC-EX-018`

Title: Add embedding example verification coverage

Goal:
- Ensure host example projects remain trustworthy as the public API evolves.

Description:
- Add build and smoke coverage for the richer embedding projects and verify that their documented run paths stay valid.

Acceptance Criteria:
- embedding example projects build in CI-style verification
- key example projects have smoke coverage
- docs and project behavior remain aligned

Dependencies:
- `PROC-EX-017`

Risks or notes:
- smoke tests should avoid becoming too slow or flaky

### `PROC-EX-019`

Title: Expand example catalog documentation and navigation

Goal:
- Make the larger example surface discoverable for different user audiences.

Description:
- Update `examples/README.md` and related docs with matrix views, scenario paths, example metadata, and clearer discovery routes for beginners, embedders, and operator-style users.

Acceptance Criteria:
- catalog includes category and scenario views
- new examples are linked from the right docs
- users can find examples by feature and by real-world scenario

Dependencies:
- `PROC-EX-001`
- `PROC-EX-003`
- `PROC-EX-011`
- `PROC-EX-013`
- `PROC-EX-017`

Risks or notes:
- documentation should not turn into an unstructured list

### `PROC-EX-020`

Title: Genericize embedding example project inputs

Goal:
- Make the richer host example projects honestly reusable instead of only working with their default scenario assumptions.

Description:
- Update the newer embedding projects so their documented CLI surface matches their actual behavior. In practice this means allowing callers to supply workflow paths together with the wait/query/resume inputs those workflows require, rather than hard-coding scenario-specific wait keys, signal types, and payloads behind a generic-looking entrypoint.

Acceptance Criteria:
- `Procedo.Example.CallbackResumeHost` accepts configurable wait identity and resume payload inputs
- `Procedo.Example.AdvancedObservability` accepts configurable workflow and resume inputs when used with resumable scenarios
- `Procedo.Example.ParityRunner` accepts configurable workflow input while preserving a sensible default
- project help/output makes it clear which arguments are optional and which are scenario-specific
- smoke coverage still passes for the default documented paths

Dependencies:
- `PROC-EX-017`
- `PROC-EX-018`

Risks or notes:
- prefer a small, explicit CLI surface over trying to make the projects into general-purpose tools

### `PROC-EX-021`

Title: Add policy and custom-store embedding example projects

Goal:
- Fill the remaining high-value gaps in the .NET embedding story.

Description:
- Add richer host examples for policy-driven hosting and custom persistence/resolver integration so embedders can see how to apply security/runtime options and non-default infrastructure boundaries in real code.

Acceptance Criteria:
- `Procedo.Example.PolicyHost` exists and demonstrates host policy configuration
- `Procedo.Example.CustomResolverStore` exists and demonstrates custom workflow resolution and/or run-state-store capability wiring
- the new projects are added to the solution, catalog, and examples README
- the documented default run paths are executed successfully

Dependencies:
- `PROC-EX-020`

Risks or notes:
- keep these examples focused on supported public APIs, not internal extension hooks

## Recommended execution order

1. `PROC-EX-001`
2. `PROC-EX-002`
3. `PROC-EX-003`
4. `PROC-EX-004`
5. `PROC-EX-005`
6. `PROC-EX-006`
7. `PROC-EX-007`
8. `PROC-EX-008`
9. `PROC-EX-009`
10. `PROC-EX-010`
11. `PROC-EX-011`
12. `PROC-EX-012`
13. `PROC-EX-013`
14. `PROC-EX-014`
15. `PROC-EX-015`
16. `PROC-EX-016`
17. `PROC-EX-017`
18. `PROC-EX-018`
19. `PROC-EX-019`

## Minimum first slice

The best first implementation slice is:

- `PROC-EX-001`
- `PROC-EX-002`
- `PROC-EX-003`
- `PROC-EX-004`
- `PROC-EX-005`
- `PROC-EX-006`

That gives Procedo:

- example inventory and governance
- explicit null semantics examples and verification
- explicit persisted parity examples and verification

## Follow-up notes

This backlog should be treated as a follow-on quality program after the core Phase 1 engine release candidate work.

The intent is not to delay release indefinitely. The intent is to raise the quality bar of the example and verification surface in a deliberate, reusable way.
