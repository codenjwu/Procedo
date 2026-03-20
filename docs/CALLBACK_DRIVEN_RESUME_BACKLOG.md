# Callback-Driven Resume Backlog

## Purpose

This document tracks the follow-up backlog for hardening Procedo's callback-driven resume support after the initial implementation.

The source-of-truth requirements remain in:

- [CALLBACK_DRIVEN_RESUME_REQUIREMENTS.md](/D:/Project/codenjwu/Procedo/docs/CALLBACK_DRIVEN_RESUME_REQUIREMENTS.md)

This backlog exists to capture implementation work discovered during design and code review.

## Scope of this backlog

This backlog focuses on four areas:

- file-store concurrency correctness
- persisted value fidelity
- public store API compatibility
- workflow-definition identity correctness during resume

These items are generic engine and hosting concerns. They must stay transport-agnostic and must not introduce product-specific behavior.

## Epic

- `PROC-CBR-HARDEN-EPIC`
  Title: Callback-Driven Resume Hardening
  Goal: Close the correctness, compatibility, and workflow-identity gaps found during post-implementation review of callback-driven resume support.
  Description: Harden file-store concurrency behavior, preserve persisted value fidelity, resolve the public store API compatibility issue, and make resume-by-identity use a stable workflow-definition identity rather than implicitly reloading the latest file contents.
  Acceptance Criteria:
  - File-store resume claiming is safe across concurrent local processes.
  - Persisted null values round-trip without semantic drift.
  - Public store-extension strategy is explicitly compatibility-safe or intentionally versioned as a breaking change.
  - Resume-by-identity resolves the intended workflow definition deterministically.
  Dependencies:
  - Existing callback-driven resume implementation
  Risks or notes:
  - The workflow-identity and API-compatibility items may affect public contracts and should be treated deliberately.

## Ordered stories

### `PROC-CBR-HARDEN-001`

Title: Add cross-process claim protection for file-backed resume

Goal:
- Ensure only one local process can successfully claim and resume a waiting run from the same state directory.

Description:
- Replace the current process-local coordination in `FileRunStateStore` with an OS-visible per-run claim mechanism, such as an exclusive lock file, so the conditional save path is safe across multiple host processes on the same machine.

Acceptance Criteria:
- Two separate processes attempting to resume the same waiting run cannot both succeed.
- The losing caller gets a clear stale/already-resumed failure.
- `TrySaveRunAsync(...)` remains deterministic and does not corrupt run-state files.
- Existing same-process behavior continues to work.

Dependencies:
- None

Risks or notes:
- This should stay scoped to the current single-node file-store model, not a distributed lock design.
- Lock-file cleanup behavior must be safe after crashes.

### `PROC-CBR-HARDEN-002`

Title: Preserve null fidelity in persisted workflow parameters and wait metadata

Goal:
- Ensure persisted run state round-trips values without changing workflow semantics.

Description:
- Update `FileRunStateStore` so `null` values in persisted workflow parameters and wait metadata remain `null` instead of being coerced to `string.Empty`. This is especially important for workflow-definition reload during resume.

Acceptance Criteria:
- Persisted `null` parameter values are read back as `null`.
- Persisted `null` wait metadata values are read back as `null`.
- Resume behavior for template and parameterized workflows does not change semantics because of persistence normalization.
- Regression tests cover parameter and metadata round-tripping.

Dependencies:
- None

Risks or notes:
- Be careful not to break existing JSON compatibility for older run files.
- Validate this against template reload behavior, not just raw serialization.

### `PROC-CBR-HARDEN-003`

Title: Add regression tests for multi-process resume contention

Goal:
- Prove the file-store claim path works under realistic concurrent resume races.

Description:
- Add integration or persistence-level tests that simulate two separate processes attempting to resume the same waiting run and verify that at most one succeeds. Keep the tests deterministic and avoid timing-only assertions where possible.

Acceptance Criteria:
- Test coverage demonstrates that only one contender can claim a waiting run.
- The losing contender receives a stale/already-resumed result.
- Tests pass reliably without flaky timing assumptions.

Dependencies:
- `PROC-CBR-HARDEN-001`

Risks or notes:
- True multi-process testing may be slower than unit tests; keep the scope focused.

### `PROC-CBR-HARDEN-004`

Title: Restore backward-compatible store extensibility for callback-resume support

Goal:
- Remove or mitigate the breaking change caused by extending `IRunStateStore`.

Description:
- Rework the callback-resume store contract so external and custom store implementations are not forced to implement new members immediately. Preferred options are a companion interface or an adapter-based capability model rather than direct expansion of `IRunStateStore`.

Acceptance Criteria:
- Existing custom `IRunStateStore` implementations remain source-compatible or have a clearly versioned migration path.
- Callback-resume capabilities are still available to stores that opt in.
- Documentation clearly explains the extension model.

Dependencies:
- None

Risks or notes:
- If the team intentionally accepts a breaking change for the next version, this story may become a compatibility-note story instead of a refactor story.
- This is a public API design decision and should be handled carefully.

### `PROC-CBR-HARDEN-005`

Title: Add compatibility coverage for store-extension behavior

Goal:
- Lock down the intended compatibility story for run-state stores.

Description:
- Add contract or compilation-level coverage that verifies the chosen store extension design behaves as intended, especially for existing and custom store implementations.

Acceptance Criteria:
- Tests or compatibility fixtures cover the chosen extension model.
- Documentation and tests agree on whether the change is additive or intentionally versioned.

Dependencies:
- `PROC-CBR-HARDEN-004`

Risks or notes:
- The exact test shape depends on whether the solution uses a companion interface, adapter, or versioned break.

### `PROC-CBR-HARDEN-006`

Title: Persist stable workflow-definition identity for waiting runs

Goal:
- Ensure resume-by-identity uses the intended workflow definition, not whatever file happens to be on disk later.

Description:
- Extend persisted workflow identity beyond `WorkflowSourcePath` and parameters so a resumed run can be matched to a stable workflow definition. Options include persisting a workflow fingerprint or hash, a versioned content snapshot, or a stronger resolver contract that can validate identity.

Acceptance Criteria:
- Waiting runs persist enough workflow identity to detect or avoid drift.
- Resume-by-identity no longer silently executes a changed workflow file.
- Failure mode is explicit if the original workflow definition cannot be reconstructed safely.

Dependencies:
- None

Risks or notes:
- This is the most important design-hardening story after concurrency.
- Avoid storing excessive redundant payload unless necessary.

### `PROC-CBR-HARDEN-007`

Title: Harden workflow-definition resolver behavior for resume

Goal:
- Make workflow resolution deterministic and explicit during callback-driven resume.

Description:
- Update `FileWorkflowDefinitionResolver` and related hosting and engine code so resolution validates the persisted workflow identity instead of blindly loading the current file contents.

Acceptance Criteria:
- Resolver checks persisted workflow identity before returning a definition.
- Resume fails clearly if the workflow file has changed incompatibly.
- Hosted file-based resume remains ergonomic for normal use.

Dependencies:
- `PROC-CBR-HARDEN-006`

Risks or notes:
- The user experience should be clear when a waiting run can no longer be resumed safely because its source changed.

### `PROC-CBR-HARDEN-008`

Title: Add workflow-drift and persisted-identity regression tests

Goal:
- Prove that callback-driven resume behaves correctly when workflow source files change after a run enters waiting state.

Description:
- Add tests that pause a run, modify the underlying workflow or template file, and verify the new workflow-identity design either reconstructs the original definition correctly or fails with an explicit drift error.

Acceptance Criteria:
- Tests cover workflow file change after wait.
- Resume does not silently execute an unintended changed workflow.
- Error messaging is clear when drift is detected.

Dependencies:
- `PROC-CBR-HARDEN-006`
- `PROC-CBR-HARDEN-007`

Risks or notes:
- This is important because the current review found a real correctness gap here.

### `PROC-CBR-HARDEN-009`

Title: Update callback-resume docs for concurrency scope, compatibility, and workflow identity

Goal:
- Keep the docs aligned with the hardened implementation and any remaining limitations.

Description:
- Update `PERSISTENCE.md`, `RUNBOOK.md`, and `API_COMPATIBILITY.md` to explain the final concurrency guarantees, store extensibility model, and workflow-definition identity behavior.

Acceptance Criteria:
- Docs clearly state the concurrency scope for the file store.
- Docs explain the compatibility story for store implementers.
- Docs explain how workflow-definition resolution behaves for waiting runs.

Dependencies:
- `PROC-CBR-HARDEN-001`
- `PROC-CBR-HARDEN-004`
- `PROC-CBR-HARDEN-006`
- `PROC-CBR-HARDEN-007`

Risks or notes:
- If some limitations remain by design, document them explicitly rather than implying stronger guarantees.

## Recommended implementation order

1. `PROC-CBR-HARDEN-001`
2. `PROC-CBR-HARDEN-002`
3. `PROC-CBR-HARDEN-003`
4. `PROC-CBR-HARDEN-006`
5. `PROC-CBR-HARDEN-007`
6. `PROC-CBR-HARDEN-008`
7. `PROC-CBR-HARDEN-004`
8. `PROC-CBR-HARDEN-005`
9. `PROC-CBR-HARDEN-009`

This order fixes the highest runtime-correctness risks first, then addresses public API compatibility and final documentation.

## Minimum practical hardening slice

If the team wants the smallest useful hardening pass first, do:

1. `PROC-CBR-HARDEN-001`
2. `PROC-CBR-HARDEN-002`
3. `PROC-CBR-HARDEN-003`
4. `PROC-CBR-HARDEN-006`
5. `PROC-CBR-HARDEN-007`
6. `PROC-CBR-HARDEN-008`

That slice addresses the most important correctness gaps before the compatibility refinement work.

## Final follow-up mini-epic

These are the final smaller review findings that remain after the main hardening pass.

### `PROC-CBR-FOLLOWUP-EPIC`

Title: Callback-Driven Resume Follow-Up Hardening

Goal:
- Close the final post-hardening gaps around resume concurrency guarantees and file-store lock-file hygiene.

Description:
- Align resume-by-`runId` behavior with the documented concurrency model and prevent lock-file litter in the built-in file store.

Acceptance Criteria:
- Resume concurrency guarantees are explicit and correct for both callback-driven resume and resume-by-`runId`.
- The built-in file store does not leave confusing lock-file artifacts behind during normal operation.

Dependencies:
- Existing callback-driven resume hardening work

Risks or notes:
- The resume guarantee story affects public expectations and should be resolved deliberately.

### `PROC-CBR-FOLLOWUP-001`

Title: Align resume-by-runId concurrency guarantees with store capability model

Goal:
- Ensure resume-by-`runId` either has real conditional-save protection or fails clearly when the store cannot provide it.

Description:
- Persisted resumes in `ProcedoWorkflowEngine` currently use the compatibility fallback in `RunStateStoreExtensions`, which silently calls `SaveRunAsync(...)` for stores that do not implement `IConditionalRunStateStore`. That weakens the documented stale-resume protection for legacy stores. Update the engine and/or compatibility helper so the behavior is explicit and safe.

Acceptance Criteria:
- Resume-by-`runId` does not silently claim concurrency safety when the underlying store lacks conditional-save support.
- The chosen behavior is one of:
  - require `IConditionalRunStateStore` for persisted resume, or
  - clearly and deliberately scope non-conditional stores to non-concurrency-safe resume behavior with matching docs and tests.
- Integration and compatibility tests cover the chosen behavior.
- Documentation matches the final behavior exactly.

Dependencies:
- Existing `IConditionalRunStateStore` capability model

Risks or notes:
- This is the higher-priority fix because it affects a correctness guarantee.
- If the team chooses a docs-only narrowing instead of a code restriction, that should be a deliberate release decision.

### `PROC-CBR-FOLLOWUP-002`

Title: Clean up file-store lock-file lifecycle

Goal:
- Prevent stale `.lock` files from accumulating in the built-in persistence directory.

Description:
- The built-in file store in `FileRunStateStore` now uses OS-visible per-run lock files for cross-process claim safety, but those files are not removed after normal save/delete flows. Update the implementation so lock files are cleaned up when safe, without weakening concurrency protection.

Acceptance Criteria:
- Normal save/update/delete flows do not leave unnecessary `.lock` files behind.
- Lock-file cleanup does not race with active lock holders.
- Crash-safe behavior remains acceptable for the single-node file-store model.
- Tests cover normal cleanup and deletion scenarios.

Dependencies:
- Existing file-store locking implementation

Risks or notes:
- The lock file itself is not harmful, but buildup creates operator confusion and state-directory clutter.
- Cleanup must be careful not to remove a lock file that another active process still relies on.

## Follow-up implementation order

1. `PROC-CBR-FOLLOWUP-001`
2. `PROC-CBR-FOLLOWUP-002`
