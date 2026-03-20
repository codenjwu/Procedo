# Architecture Hardening Backlog

## Purpose

This document tracks the follow-up architecture and correctness work discovered during the full post-Phase-1 review of Procedo.

The review highlighted four main areas:

- execution-path parity
- null-fidelity consistency
- `${{ each }}` contract alignment
- resume observability semantics

These items should be treated as engine-level hardening work. They are not product-specific and should remain compatible with Procedo's single-node Phase 1 positioning.

## Epic

- `PROC-ARCH-HARDEN-EPIC`
  Title: Post-Phase-1 Architecture Hardening
  Goal: Align persisted execution, DSL/runtime semantics, and observability behavior with the documented Procedo Phase 1 contract.
  Description: Fix the remaining architecture inconsistencies found in the review so persisted execution behaves like the main engine path, null values remain stable across parsing/template/execution, `${{ each }}` semantics are explicit, and resume observability is unambiguous.
  Acceptance Criteria:
  - persisted and non-persisted execution honor the same scheduler/runtime policies
  - null values round-trip consistently across DSL, templates, runtime inputs, and persistence
  - `${{ each }}` behavior matches the documented contract
  - resume/replay events are semantically clear for consumers
  Dependencies:
  - current Phase 1 implementation
  Risks or notes:
  - the highest-risk item is execution-path unification because it touches core runtime behavior

## Ordered Stories

### `PROC-ARCH-001`

Title: Unify persisted execution with scheduler policy behavior

Goal:
- Make persisted execution honor the same retry, timeout, continue-on-error, and parallelism behavior as normal execution.

Description:
- `ProcedoWorkflowEngine` currently maintains a separate persisted execution loop instead of reusing the core scheduler behavior in `WorkflowScheduler`. Refactor so both normal and persisted execution share the same runtime policy semantics.

Acceptance Criteria:
- persisted execution honors step retries
- persisted execution honors step timeouts
- persisted execution honors `continue_on_error`
- persisted execution honors `max_parallelism`
- persisted and non-persisted execution produce equivalent behavior for the same workflow aside from persistence-specific state handling

Dependencies:
- None

Risks or notes:
- this is the highest-priority correctness item
- prefer extracting shared execution logic rather than duplicating scheduler rules again

### `PROC-ARCH-002`

Title: Add persisted-vs-non-persisted execution parity tests

Goal:
- Prove that persisted and non-persisted runs behave the same for core runtime policies.

Description:
- Add focused unit and integration tests comparing behavior across execution modes for retries, timeouts, `continue_on_error`, max parallelism, wait/resume, and failure propagation.

Acceptance Criteria:
- tests cover retry parity
- tests cover timeout parity
- tests cover `continue_on_error` parity
- tests cover `max_parallelism` parity
- tests cover wait/resume parity

Dependencies:
- `PROC-ARCH-001`

Risks or notes:
- keep tests scenario-focused so they stay readable and diagnostic

### `PROC-ARCH-003`

Title: Preserve null fidelity across YAML parsing

Goal:
- Stop coercing YAML and template `null` values into empty strings during parsing.

Description:
- `YamlWorkflowParser` currently normalizes `null` to `string.Empty`. Update parsing so `null` remains `null` in parameter values, variables, and `with:` payloads unless a later explicit coercion rule applies.

Acceptance Criteria:
- YAML `null` values remain `null` after parsing
- parameter defaults may remain `null`
- `with:` object values may remain `null`
- no silent conversion to `""` happens at parse time

Dependencies:
- None

Risks or notes:
- this may affect existing tests that implicitly depended on empty-string coercion

### `PROC-ARCH-004`

Title: Preserve null fidelity across template cloning and overrides

Goal:
- Ensure template merge and parameter override paths do not reintroduce null coercion.

Description:
- `WorkflowTemplateLoader` currently converts `null` to `string.Empty` in clone paths. Update cloning and merge behavior so null values survive template inheritance and runtime override application.

Acceptance Criteria:
- child workflow null overrides remain `null`
- cloned parameter values remain `null`
- cloned variable values remain `null`
- template merge behavior matches parser and persistence null semantics

Dependencies:
- `PROC-ARCH-003`

Risks or notes:
- validate this with template + parameter + persistence combinations

### `PROC-ARCH-005`

Title: Preserve null fidelity through expression and input resolution

Goal:
- Ensure runtime input binding does not silently convert null values into empty strings.

Description:
- `ExpressionResolver` currently returns `string.Empty` for `null` values in `ResolveValue`. Update the resolver so nulls remain null unless an expression function or string interpolation explicitly converts them.

Acceptance Criteria:
- direct object, array, and input null values remain `null`
- string interpolation still renders safely
- runtime condition evaluation has defined null behavior
- step inputs preserve nulls where possible

Dependencies:
- `PROC-ARCH-003`

Risks or notes:
- document the final null semantics clearly in DSL/validation docs

### `PROC-ARCH-006`

Title: Add end-to-end null semantics tests

Goal:
- Lock down null handling across DSL, templates, runtime inputs, and persistence.

Description:
- Add tests covering parse -> template merge -> execution -> persistence -> resume so null handling is stable and predictable.

Acceptance Criteria:
- tests cover YAML parameter nulls
- tests cover template override nulls
- tests cover `with:` null values
- tests cover persisted and resumed null round-trip

Dependencies:
- `PROC-ARCH-003`
- `PROC-ARCH-004`
- `PROC-ARCH-005`

Risks or notes:
- important because null handling now spans multiple subsystems

### `PROC-ARCH-007`

Title: Align `${{ each }}` implementation with documented array-only contract

Goal:
- Make `${{ each }}` behavior match the documented Phase 1 DSL contract.

Description:
- `ConditionalYamlPreprocessor` currently accepts any non-string `IEnumerable`. Tighten it so only array and list-style values are valid, or deliberately broaden the docs and tests if object iteration is wanted.

Acceptance Criteria:
- `${{ each }}` behavior is deterministic and explicitly defined
- docs and implementation agree
- invalid non-array targets fail clearly if array-only is kept

Dependencies:
- None

Risks or notes:
- recommended direction for Phase 1 is enforcing array-only semantics

### `PROC-ARCH-008`

Title: Add `${{ each }}` contract tests for unsupported object iteration

Goal:
- Prevent future drift between docs and code for iteration semantics.

Description:
- Add tests that explicitly cover array success cases and object or dictionary failure cases if array-only remains the contract.

Acceptance Criteria:
- array iteration passes
- string iteration fails
- object and dictionary iteration fail clearly, or are explicitly covered as supported if the contract changes

Dependencies:
- `PROC-ARCH-007`

Risks or notes:
- keep the tests close to the public DSL story, not just internal implementation details

### `PROC-ARCH-009`

Title: Clarify resume replay event semantics

Goal:
- Make resumed-run observability distinguish true skipped steps from replayed completed steps.

Description:
- `ProcedoWorkflowEngine` currently emits `StepSkipped` when replaying already-completed steps during resume. Rework the event model so observability consumers can tell “previously completed before resume” apart from “condition false” or “intentionally skipped.”

Acceptance Criteria:
- event semantics are unambiguous on resumed runs
- docs match the emitted behavior
- event consumers can distinguish replay from true skip

Dependencies:
- None

Risks or notes:
- this may require either different event types or clearer use of existing fields like `Resumed`

### `PROC-ARCH-010`

Title: Add resume observability regression tests

Goal:
- Lock down the intended event semantics for resumed runs.

Description:
- Add focused tests around resumed workflows, replayed completed steps, runtime-skipped steps, and waiting/resumed flows so event consumers have a stable contract.

Acceptance Criteria:
- tests cover resumed previously-completed steps
- tests cover runtime `condition:` skipped steps
- tests cover waiting/resumed event ordering
- docs and tests agree on meaning

Dependencies:
- `PROC-ARCH-009`

Risks or notes:
- especially important if external consumers will read JSONL events

### `PROC-ARCH-011`

Title: Update docs for execution parity, null semantics, iteration contract, and resume event behavior

Goal:
- Keep the user-facing docs aligned with the corrected implementation.

Description:
- Update `PERSISTENCE.md`, `TEMPLATES.md`, `VALIDATION.md`, `OBSERVABILITY.md`, and `CONTROL_FLOW_RECIPES.md` so the documented contract matches the fixed implementation.

Acceptance Criteria:
- docs no longer overstate or misstate behavior
- persisted execution policy behavior is described correctly
- null semantics are documented clearly
- `${{ each }}` contract is explicit
- resume event semantics are documented clearly

Dependencies:
- `PROC-ARCH-001`
- `PROC-ARCH-006`
- `PROC-ARCH-007`
- `PROC-ARCH-009`

Risks or notes:
- doc updates should come after final semantics are settled

## Recommended Order

1. `PROC-ARCH-001`
2. `PROC-ARCH-002`
3. `PROC-ARCH-003`
4. `PROC-ARCH-004`
5. `PROC-ARCH-005`
6. `PROC-ARCH-006`
7. `PROC-ARCH-007`
8. `PROC-ARCH-008`
9. `PROC-ARCH-009`
10. `PROC-ARCH-010`
11. `PROC-ARCH-011`

## Minimum Must-Fix Slice Before Release

If the team wants the smallest useful hardening pass first, do:

1. `PROC-ARCH-001`
2. `PROC-ARCH-002`
3. `PROC-ARCH-003`
4. `PROC-ARCH-004`
5. `PROC-ARCH-005`
6. `PROC-ARCH-006`

That is the core correctness slice.
