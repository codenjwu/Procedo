# Production Readiness Plan

This document defines Procedo's path to a production-ready single-node release (`v1.0`) and a follow-up Phase 2 roadmap.

## Phase 1: Single-Node Production (`v1.0`)

Goal: make Procedo safe and predictable for real workloads on one machine.

### Readiness objectives

- Reliability policies are complete and configurable.
- Runtime/API contracts are stable and versioned.
- Test suite proves failure handling and recovery, not only happy paths.
- Operational docs/runbooks exist for day-2 operations.
- Common flow/control and expression authoring scenarios are supported without requiring users to drop into custom code for every branch.

### Production checklist (go-live gate)

#### 1. Reliability policies

- [x] Step timeout policy (default + per-step override)
- [x] Workflow cancellation support
- [x] Retry policy (max retries, backoff, jitter)
- [x] Failure policy (`fail-fast` / `continue-on-error`)
- [x] Max parallelism controls (global + per-job if needed)
- [x] Deterministic retry classification (retryable vs non-retryable errors)
- [x] Idempotency guidance for plugin authors

#### 2. API/runtime operability

- [x] Freeze CLI flags and semantics for v1
- [x] Define DSL compatibility policy (minor/patch guarantees)
- [x] Add DSL flow/control support for `${{ if }}`, `${{ elseif }}`, `${{ else }}`, and `${{ each }}`
- [x] Add expression/condition function support for `eq`, `ne`, `and`, `or`, `not`, `contains`, `startsWith`, `endsWith`, `in`, and `format`
- [x] Define and document template-time vs runtime evaluation semantics
- [x] Define event schema compatibility policy (`SchemaVersion` lifecycle)
- [x] Finish standardizing remaining runtime/validation error-code and message gaps
- [x] Config layering model (defaults + file + env + CLI precedence)
- [x] Release notes template + upgrade notes template
- [x] Default plugin bundle registration for `system.*` and `demo.*` examples

#### 3. Production test bar

- [x] Failure-path integration tests (timeout/retry/cancel)
- [x] Persistence recovery tests (kill/restart/resume)
- [x] Concurrency/load tests on single node
- [x] Soak test (long-running stability)
- [x] Regression tests for DSL/event backward compatibility
- [x] Flaky test policy and quarantine process
- [x] Demo plugin unit and integration suites for complex workflow scenarios
- [x] Add compatibility coverage for flow/control expansion and expression evaluation

#### 4. Operability docs

- [x] Runbook: start/resume/cancel/recover
- [x] Troubleshooting guide (common failures + fixes)
- [x] Capacity guidance for single-node tuning
- [x] Plugin authoring contract (timeouts, retries, outputs, logging)

### Phase 1 completion status

- Status: ready for Phase 1 release candidate verification and packaging.
- Verification: see `docs/PHASE1_RELEASE_CHECKLIST.md` for test evidence and go/no-go record.
- Demo workflows: advanced `demo.*` examples are executable by default through runtime plugin registration.
- Public NuGet packaging now centers on the five-package surface documented in `docs/PACKAGE_GUIDE.md`.

## Phase 2: Post-v1 roadmap

### 1. Persistence hardening

- Transactional database-backed run state store
- State schema migration/versioning strategy
- Corruption detection and recovery tooling

### 2. Security and isolation

- Plugin trust model and sandbox strategy
- Secret provider integration
- Plugin package signing/allowlist policy
- Stronger audit trail coverage

### 3. Observability maturity

- Metrics model (latency, retries, failures, throughput)
- Trace/correlation IDs across run and step boundaries
- Alert thresholds and dashboard standards
- Additional pluggable sinks (for example OTLP/central logging backends)

### 4. Distributed orchestration (when needed)

- Queue-backed scheduling and worker coordination
- Lease-based step claiming/failover
- Multi-node execution recovery and rebalancing
## Phase 2 TODO checklist

- [ ] Implement transactional database-backed run state store and migration/versioning strategy.
- [ ] Implement plugin trust model, signing/allowlist, and stronger isolation boundaries.
- [ ] Add metrics/tracing standards with alert thresholds and dashboards.
- [ ] Add production-grade pluggable sinks (for example OTLP/centralized logging).
- [ ] Design and prototype queue-backed distributed orchestration and failover leasing.

## Suggested milestones

- `v1.0-alpha`: core reliability policies + initial failure-path tests
- `v1.0-rc`: compatibility policies + soak/load + operational runbook
- `v1.0`: all Phase 1 checklist items complete and verified
