# Example Strategy

This document defines how Procedo should grow its example catalog going forward.

Examples are a core product surface for Procedo, not just supporting material. They should teach the engine, prove its behavior, and give embedders confidence that real workflows can be built on top of it.

## Goals

The Procedo example catalog should:

- teach one concept at a time for new users
- prove that features compose cleanly in realistic workflows
- demonstrate the intended public package and embedding story
- serve as executable documentation
- provide stable reference scenarios for regression testing

## Example principles

Every important engine feature should have:

- one focused teaching example
- one composed example that combines it with nearby features
- one realistic scenario example
- one automated verification path

Examples should be:

- runnable
- named consistently
- discoverable from `examples/README.md`
- categorized by audience and purpose
- backed by tests where practical

## Example taxonomy

Procedo examples should be organized into four tracks.

### 1. Foundation examples

Purpose:

- teach one feature or one narrow behavior
- be easy to read in one sitting
- be safe to copy and adapt

Typical size:

- 1 stage
- 1 job
- a handful of steps

Topics to cover:

- hello world
- dependencies
- outputs and expressions
- retries
- timeouts
- `continue_on_error`
- `max_parallelism`
- runtime `condition:`
- expression functions
- wait/resume
- callback-driven resume basics
- templates
- parameter validation
- null semantics
- skipped-step semantics

Recommended additions:

- `63_null_semantics_showcase.yaml`
- `64_template_null_override_demo.yaml`
- `65_persisted_null_resume_demo.yaml`
- `66_retry_parity_demo.yaml`
- `67_timeout_parity_demo.yaml`
- `68_continue_on_error_parity_demo.yaml`
- `69_max_parallelism_parity_demo.yaml`
- `70_wait_resume_parity_demo.yaml`
- `71_callback_resume_identity_demo.yaml`
- `72_callback_resume_two_cycle_demo.yaml`
- `73_callback_resume_snapshot_safety_demo.yaml`

Still useful follow-up additions:

- `array_and_object_parameters_showcase.yaml`
- `expression_function_matrix.yaml`
- `skip_and_condition_showcase.yaml`
- `retry_timeout_interaction.yaml`
- `parallelism_scope_showcase.yaml`

### 2. Composition examples

Purpose:

- show that multiple features work together
- capture the real Procedo authoring style
- prove that new features are not isolated demos only

Topics to cover:

- templates + `condition:` + `${{ each }}`
- templates + persistence + resume
- templates + null overrides + structured parameters
- callback-driven resume + persisted workflow snapshot behavior
- multi-stage + runtime gating + wait/resume
- fan-out/fan-in + retries + skipped branches
- secure runtime + persistence + observability

Recommended additions:

- `74_control_flow_array_iteration_demo.yaml`
- `75_mixed_template_runtime_control_flow_demo.yaml`
- `77_template_null_condition_audit_demo.yaml`
- `78_template_persisted_resume_observability_demo.yaml`
- `79_template_artifact_bundle_composition_demo.yaml`

Still useful follow-up additions:

- `callback_resume_with_template.yaml`
- `fanout_retry_skip_matrix.yaml`
- `secure_runtime_persistence_observability.yaml`

### 3. Real-world scenario packs

Purpose:

- demonstrate what Procedo is actually for
- show operator-facing workflows, not only feature samples
- give users realistic starting points

Each scenario family should ideally include:

- one happy-path example
- one degraded or failure-path example
- one recovery or resume-path example

Recommended scenario families:

- release orchestration
- incident response
- data and ETL operations
- compliance and audit workflows
- model promotion
- maintenance and runbook automation
- approval-heavy enterprise workflows
- disaster recovery drills

Recommended additions:

- `80_release_train_canary_approval.yaml`
- `81_release_train_recovery_demo.yaml`
- `82_incident_triage_severity_branching.yaml`
- `83_maintenance_window_runbook_demo.yaml`
- `84_etl_reconciliation_audit_demo.yaml`
- `85_compliance_audit_bundle_demo.yaml`
- `86_model_promotion_governance_demo.yaml`

Still useful follow-up additions:

- `disaster_recovery_drill.yaml`
- `batch_ingestion_retry_matrix.yaml`
- another degraded-path ETL variant if needed
- `87_disaster_recovery_drill.yaml`
- `88_batch_ingestion_retry_matrix.yaml`

### 4. Embedding example projects

Purpose:

- prove the .NET embedding story
- show how the public packages are intended to be used
- demonstrate host configuration, registration, and policy patterns

Recommended additions:

- `Procedo.Example.CallbackResumeHost`
- `Procedo.Example.AdvancedObservability`
- `Procedo.Example.ParityRunner`
- `Procedo.Example.PolicyHost`
- `Procedo.Example.CustomResolverStore`

Still useful follow-up additions:

- `Procedo.Example.EnterpriseOps`

## Example metadata model

Every example should have a documented classification in the catalog.

Recommended metadata fields:

- category
- audience
- expected outcome
- required plugins
- requires persistence
- requires resume
- produces artifacts
- related docs
- test coverage id

Expected outcome should be one of:

- success
- validation failure
- runtime failure
- waiting
- resume required

## Catalog expectations

`examples/README.md` should keep a clear path for:

- first runnable example
- first embedding example
- first persistence example
- first template/control-flow example
- first enterprise/operator example

The catalog should also maintain:

- a good-first-picks section
- an audience-routing section
- a feature matrix view
- a real-world scenario view
- a host embedding projects view

In practice this means `examples/README.md` should help users navigate the catalog by:

- audience
- feature area
- real-world scenario family
- host embedding pattern

## Naming rules

YAML examples should continue the numbered catalog approach:

- ordered from simple to complex
- stable filenames once published
- descriptive enough to understand without opening the file

Project examples should use:

- `Procedo.Example.*`

Template helper files should live under:

- `examples/templates`

## Example quality bar

A high-quality Procedo example should:

- execute with the documented command
- avoid hidden prerequisites unless clearly called out
- create predictable outputs when it produces artifacts
- be understandable without reading engine internals
- demonstrate the supported public authoring model

When examples intentionally fail, they should fail for one clear reason and state that reason in the catalog.

## Example milestones

### Milestone 1: Semantics coverage

Add focused examples for:

- null semantics
- array-only `${{ each }}`
- persisted parity basics
- callback-driven resume basics

### Milestone 2: Composition coverage

Add medium-complexity examples for:

- templates + persistence
- templates + runtime conditions
- callback resume + templates
- secure runtime + observability + persistence

### Milestone 3: Scenario packs

Add realistic operator scenarios across:

- release
- incident
- data
- audit
- model promotion
- maintenance

### Milestone 4: Embedding maturity

Add host-level example projects that demonstrate:

- callback resume
- policy configuration
- advanced sinks
- parity comparison

## Success criteria

The example strategy is succeeding when:

- users can find an example for every major feature area
- advanced behavior is demonstrated in realistic combinations
- examples are routinely executed in automated verification
- docs, examples, and tests tell the same product story

Implementation tracking for this strategy lives in:

- `docs/EXAMPLE_AND_TEST_BACKLOG.md`
