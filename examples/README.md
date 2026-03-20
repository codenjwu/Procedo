# Examples Catalog

These examples are ordered from simple to complex.

If you just want to get moving:

1. Run `examples/01_hello_echo.yaml` with the CLI host.
2. Run `examples/Procedo.Example.Basic` if you want to see embedding code.
3. Run `examples/Procedo.Example.Catalog -- --list` if you want one entry point for everything.

## Fast paths

Smallest runnable workflow:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

Smallest embedding example:

```powershell
dotnet run --project examples/Procedo.Example.Basic
```

Umbrella launcher:

```powershell
dotnet run --project examples/Procedo.Example.Catalog -- --list
dotnet run --project examples/Procedo.Example.Catalog -- --run basic
dotnet run --project examples/Procedo.Example.Catalog -- --run all
```

## Audience routes

If you are new to Procedo CLI authoring:

- `examples/01_hello_echo.yaml`
- `examples/05_outputs_and_expressions.yaml`
- `examples/16_persistence_resume_happy_path.yaml`

If you are learning templates and control flow:

- `examples/48_template_parameters_demo.yaml`
- `examples/53_runtime_condition_demo.yaml`
- `examples/59_branching_operator_showcase.yaml`
- `examples/74_control_flow_array_iteration_demo.yaml`
- `examples/75_mixed_template_runtime_control_flow_demo.yaml`

If you are evaluating persistence, wait/resume, and callback-driven flows:

- `examples/45_wait_signal_demo.yaml`
- `examples/56_change_window_release_demo.yaml`
- `examples/70_wait_resume_parity_demo.yaml`
- `examples/71_callback_resume_identity_demo.yaml`
- `examples/78_template_persisted_resume_observability_demo.yaml`
- `examples/80_release_train_canary_approval.yaml`
- `examples/83_maintenance_window_runbook_demo.yaml`
- `examples/86_model_promotion_governance_demo.yaml`

If you want realistic operator scenarios:

- `examples/56_change_window_release_demo.yaml`
- `examples/57_incident_evidence_bundle_demo.yaml`
- `examples/80_release_train_canary_approval.yaml`
- `examples/81_release_train_recovery_demo.yaml`
- `examples/82_incident_triage_severity_branching.yaml`
- `examples/83_maintenance_window_runbook_demo.yaml`
- `examples/84_etl_reconciliation_audit_demo.yaml`
- `examples/85_compliance_audit_bundle_demo.yaml`
- `examples/86_model_promotion_governance_demo.yaml`

If you are embedding Procedo in a .NET host:

- `examples/Procedo.Example.Basic`
- `examples/Procedo.Example.DependencyInjection`
- `examples/Procedo.Example.CallbackResumeHost`
- `examples/Procedo.Example.AdvancedObservability`
- `examples/Procedo.Example.ParityRunner`
- `examples/Procedo.Example.PolicyHost`
- `examples/Procedo.Example.CustomResolverStore`

Embedding examples with configurable CLI surfaces:

- `examples/Procedo.Example.CallbackResumeHost -- --workflow <path> --wait-key <key> --signal-type <signal>`
- `examples/Procedo.Example.AdvancedObservability -- --workflow <path> --resume-signal <signal>`
- `examples/Procedo.Example.ParityRunner -- --workflow <path>`
- `examples/Procedo.Example.PolicyHost -- --artifacts-dir <path>`
- `examples/Procedo.Example.CustomResolverStore -- --workflow <path> --wait-key <key> --signal-type <signal>`

## Feature map

Runtime conditions and expression functions:

- `examples/53_runtime_condition_demo.yaml`
- `examples/58_runtime_expression_function_showcase.yaml`
- `examples/75_mixed_template_runtime_control_flow_demo.yaml`

Template branching and iteration:

- `examples/54_template_runtime_condition_demo.yaml`
- `examples/59_branching_operator_showcase.yaml`
- `examples/60_template_branching_release_pack_demo.yaml`
- `examples/74_control_flow_array_iteration_demo.yaml`

Null semantics and structured inputs:

- `examples/63_null_semantics_showcase.yaml`
- `examples/64_template_null_override_demo.yaml`
- `examples/65_persisted_null_resume_demo.yaml`
- `examples/77_template_null_condition_audit_demo.yaml`
- `examples/85_compliance_audit_bundle_demo.yaml`

Persistence, resume, and callback-driven resume:

- `examples/16_persistence_resume_happy_path.yaml`
- `examples/45_wait_signal_demo.yaml`
- `examples/56_change_window_release_demo.yaml`
- `examples/61_template_wait_resume_release_pack_demo.yaml`
- `examples/70_wait_resume_parity_demo.yaml`
- `examples/71_callback_resume_identity_demo.yaml`
- `examples/72_callback_resume_two_cycle_demo.yaml`
- `examples/73_callback_resume_snapshot_safety_demo.yaml`
- `examples/80_release_train_canary_approval.yaml`
- `examples/83_maintenance_window_runbook_demo.yaml`
- `examples/86_model_promotion_governance_demo.yaml`

Execution-policy parity and resilience:

- `examples/66_retry_parity_demo.yaml`
- `examples/67_timeout_parity_demo.yaml`
- `examples/68_continue_on_error_parity_demo.yaml`
- `examples/69_max_parallelism_parity_demo.yaml`

Artifact packaging and handoff outputs:

- `examples/50_comprehensive_template_release_demo.yaml`
- `examples/57_incident_evidence_bundle_demo.yaml`
- `examples/79_template_artifact_bundle_composition_demo.yaml`
- `examples/80_release_train_canary_approval.yaml`
- `examples/81_release_train_recovery_demo.yaml`
- `examples/84_etl_reconciliation_audit_demo.yaml`
- `examples/85_compliance_audit_bundle_demo.yaml`
- `examples/86_model_promotion_governance_demo.yaml`

## Scenario view

Release and promotion:

- `examples/56_change_window_release_demo.yaml`
- `examples/60_template_branching_release_pack_demo.yaml`
- `examples/61_template_wait_resume_release_pack_demo.yaml`
- `examples/62_template_multi_stage_promotion_demo.yaml`
- `examples/80_release_train_canary_approval.yaml`
- `examples/81_release_train_recovery_demo.yaml`
- `examples/86_model_promotion_governance_demo.yaml`

Incident, evidence, and maintenance:

- `examples/57_incident_evidence_bundle_demo.yaml`
- `examples/82_incident_triage_severity_branching.yaml`
- `examples/83_maintenance_window_runbook_demo.yaml`

Data, ETL, and audit:

- `examples/25_data_platform_full_pipeline.yaml`
- `examples/27_multi_source_etl_reconciliation.yaml`
- `examples/84_etl_reconciliation_audit_demo.yaml`
- `examples/85_compliance_audit_bundle_demo.yaml`

## Runnable with built-in plugins (`system.*` + `demo.*`)

- `hello_pipeline.yaml`
- `01_hello_echo.yaml`
- `02_linear_depends_on.yaml`
- `03_fan_out_fan_in.yaml`
- `04_multi_stage_multi_job.yaml`
- `05_outputs_and_expressions.yaml`
- `06_vars_expression_via_step.yaml`
- `07_job_max_parallelism.yaml`
- `08_workflow_job_parallel_override.yaml`
- `13_missing_plugin_validation_error.yaml` (validation example)
- `14_cycle_dependency_validation_error.yaml` (validation example)
- `15_unknown_dependency_validation_error.yaml` (validation example)
- `16_persistence_resume_happy_path.yaml`
- `18_observability_console_events.yaml`
- `19_observability_jsonl_events.yaml`
- `20_config_precedence_demo.yaml`
- `22_contract_smoke.yaml`
- `23_large_dag_stress.yaml`
- `31_system_toolbox_demo.yaml`
- `32_system_file_ops_demo.yaml`
- `33_system_http_demo.yaml`
- `34_system_encoding_hash_demo.yaml`
- `35_system_archive_demo.yaml`
- `36_system_directory_demo.yaml`
- `37_system_json_demo.yaml`
- `38_system_csv_demo.yaml`
- `39_system_xml_demo.yaml`
- `40_system_process_demo.yaml`
- `42_dependency_injection_demo.yaml`
- `43_secure_runtime_allowed.yaml`
- `44_secure_runtime_blocked_process.yaml`
- `45_wait_signal_demo.yaml`
- `46_wait_resume_observability.yaml`
- `47_wait_file_demo.yaml`
- `48_template_parameters_demo.yaml` (template + parameters + workflow variables)
- `49_parameter_schema_validation_demo.yaml` (richer parameter schema validation)
- `50_comprehensive_template_release_demo.yaml` (template + richer schema + file/json/csv/archive/hash flow)
- `51_comprehensive_system_bundle_demo.yaml` (single-job built-in system workflow with json/csv/archive/hash flow)
- `52_comprehensive_wait_resume_bundle_demo.yaml` (workspace prep + wait/resume + bundle packaging)
- `53_runtime_condition_demo.yaml` (runtime `condition:` with boolean/string/list functions)
- `54_template_runtime_condition_demo.yaml` (template-time branching plus runtime `condition:` in one flow)
- `55_persistence_condition_skip_demo.yaml` (persisted run state showing a real skipped step)
- `56_change_window_release_demo.yaml` (realistic release-approval flow with wait/resume and packaged handoff artifacts)
- `57_incident_evidence_bundle_demo.yaml` (incident-response evidence collection with logs, metadata, timeline, and bundle hash)
- `58_runtime_expression_function_showcase.yaml` (focused runtime `condition:` showcase for `or`, `not`, `endsWith`, `in`, `ne`, `contains`, and `format`)
- `59_branching_operator_showcase.yaml` (focused `${{ if }}` / `${{ elseif }}` / `${{ else }}` / `${{ each }}` showcase plus runtime region gating)
- `60_template_branching_release_pack_demo.yaml` (template-driven release pack with branching, `${{ each }}`, runtime gating, and archive/hash artifacts)
- `61_template_wait_resume_release_pack_demo.yaml` (template-driven operator flow with branching, runtime gating, persisted wait/resume, and approval bundle packaging)
- `62_template_multi_stage_promotion_demo.yaml` (multi-stage promotion template with branching, runtime gating, persisted approval, and final promotion bundle)
- `63_null_semantics_showcase.yaml` (focused `null` vs `""` vs `"null"` walkthrough using structured JSON output)
- `64_template_null_override_demo.yaml` (template inheritance plus null override semantics for parameter values and nested objects)
- `65_persisted_null_resume_demo.yaml` (persisted wait/resume flow proving null values survive run-state round-trip)
- `66_retry_parity_demo.yaml` (retry parity walkthrough with the same success artifact in persisted and non-persisted runs)
- `67_timeout_parity_demo.yaml` (timeout parity walkthrough for matching failure semantics across execution modes)
- `68_continue_on_error_parity_demo.yaml` (continue-on-error parity walkthrough with sibling work preserved in both modes)
- `69_max_parallelism_parity_demo.yaml` (bounded-concurrency parity walkthrough showing a two-wide sleep batch)
- `70_wait_resume_parity_demo.yaml` (wait/resume parity walkthrough for the persisted resume path and payload capture)
- `71_callback_resume_identity_demo.yaml` (callback-driven resume basics with active wait query plus resume-by-wait-identity)
- `72_callback_resume_two_cycle_demo.yaml` (two callback-driven wait/resume cycles in the same persisted run)
- `73_callback_resume_snapshot_safety_demo.yaml` (callback-driven resume using the persisted workflow snapshot instead of changed source text)
- `74_control_flow_array_iteration_demo.yaml` (array-only `${{ each }}` plus runtime region gating in one focused control-flow example)
- `75_mixed_template_runtime_control_flow_demo.yaml` (template-time branching plus runtime gating with structured metadata and hotfix smoke targeting)
- `76_each_object_iteration_validation_error.yaml` (unsupported `${{ each }}` object-target example; expected parse/validation failure)
- `77_template_null_condition_audit_demo.yaml` (medium-complexity template composition with null overrides, runtime gating, and a JSON audit artifact)
- `78_template_persisted_resume_observability_demo.yaml` (template-driven persisted approval flow with runtime gating and a resumed summary artifact)
- `79_template_artifact_bundle_composition_demo.yaml` (template composition with region gating, manifest generation, bundle packaging, and bundle hashing)
- `80_release_train_canary_approval.yaml` (release-train scenario with canary evidence, persisted approval wait/resume, rollout notes, and packaged release output)
- `81_release_train_recovery_demo.yaml` (release-train recovery scenario with rejected canary handling, rollback packaging, and recovery notes)
- `82_incident_triage_severity_branching.yaml` (incident scenario with severity routing, branching containment actions, and packaged evidence)
- `83_maintenance_window_runbook_demo.yaml` (maintenance scenario with persisted start-gate wait/resume, checklist artifacts, and packaged runbook output)
- `84_etl_reconciliation_audit_demo.yaml` (ETL reconciliation scenario with mismatch reporting, reconciliation CSV output, and packaged audit evidence)
- `85_compliance_audit_bundle_demo.yaml` (compliance scenario with control checklist evidence, null-aware exception handling, and packaged audit output)
- `86_model_promotion_governance_demo.yaml` (model-promotion scenario with persisted governance approval, regional rollout notes, and packaged promotion output)

## Runnable now with demo plugin enabled

- `09_retry_transient.yaml` (`demo.flaky`)
- `10_timeout_failure.yaml` (`demo.sleep`)
- `11_continue_on_error_false.yaml` (`demo.fail`)
- `12_continue_on_error_true.yaml` (`demo.fail`)
- `17_persistence_resume_after_failure.yaml` (`demo.fail_once`)
- `21_cancellation_demo.yaml` (`demo.cancel`)
- `24_end_to_end_reference.yaml` (`demo.flaky`)
- `25_data_platform_full_pipeline.yaml` (`demo.quality`)
- `26_branched_release_train.yaml` (`demo.fail`)
- `27_multi_source_etl_reconciliation.yaml`
- `28_ml_feature_pipeline.yaml` (`demo.score`)
- `29_finops_daily_close.yaml` (`demo.fail`)
- `30_enterprise_reference_pipeline.yaml` (`demo.flaky`, `demo.quality`, `demo.score`)

## Example consumer projects

- `examples/Procedo.Example.Basic`: direct parser + validation + engine usage pattern.
- `examples/Procedo.Example.Extensible`: `ProcedoHostBuilder` extension pattern for options/configuration.
- `examples/Procedo.Example.CustomSteps`: demonstrates all three app-level custom registration modes: delegate registration, DI-backed `IProcedoStep` activation, method binding, input aliases, and flat POCO binding.
- `examples/Procedo.Example.ControlFlow`: runs the focused runtime-condition and branching/iteration showcase workflows.
- `examples/Procedo.Example.DependencyInjection`: demonstrates the `Procedo.Extensions.DependencyInjection` builder over `IServiceCollection`.
- `examples/Procedo.Example.Templates`: demonstrates file-based workflow templates with runtime parameter overrides.
- `examples/Procedo.Example.TemplateReleasePack`: runs the template-driven release-pack scenario with branching and artifact packaging.
- `examples/Procedo.Example.TemplateWaitResume`: runs the template-driven approval pause/resume scenario end to end.
- `examples/Procedo.Example.MultiStagePromotion`: runs the multi-stage promotion scenario with persisted approval and final packaging.
- `examples/Procedo.Example.SecureRuntime`: demonstrates a locked-down host using `SystemPluginSecurityOptions` with an allowed file operation and a blocked process execution.
- `examples/Procedo.Example.WaitResume`: demonstrates a generic wait -> persisted pause -> resume signal -> continue flow using `system.wait_signal`.
- `examples/Procedo.Example.WaitResumeObservability`: demonstrates wait/resume with console + JSONL observability traces.
- `examples/Procedo.Example.Validation`: validates multiple workflows (valid + invalid) and prints structured issues.
- `examples/Procedo.Example.PersistenceResume`: demonstrates local state persistence and resume flow (`fail_once` then resume).
- `examples/Procedo.Example.Observability`: emits execution events to console and JSONL sink.
- `examples/Procedo.Example.ScenarioPack`: runs a curated sequence from simple to enterprise pipeline.
- `examples/Procedo.Example.Catalog`: umbrella launcher for all example projects and YAML catalog suites.
- `examples/Procedo.Example.Catalog.Foundation`: runs foundational YAML scenarios expected to succeed.
- `examples/Procedo.Example.Catalog.Resilience`: runs retry/timeout/cancel/failure-resilience YAML scenarios with expected outcomes.
- `examples/Procedo.Example.Catalog.Enterprise`: runs advanced enterprise YAML scenarios (success + expected failures + expected validation failure).

## Good first picks

- `examples/01_hello_echo.yaml`: smallest runnable workflow
- `examples/05_outputs_and_expressions.yaml`: outputs and expression binding
- `condition:` is available for runtime step gating with functions like `eq(...)`, `and(...)`, `contains(...)`, and `format(...)`
- `examples/16_persistence_resume_happy_path.yaml`: persistence and resume
- `examples/48_template_parameters_demo.yaml`: templates, parameters, and workflow variables
- `examples/49_parameter_schema_validation_demo.yaml`: richer parameter validation
- `examples/53_runtime_condition_demo.yaml`: runtime step gating with `condition:`
- `examples/54_template_runtime_condition_demo.yaml`: template-time branching plus runtime gating
- `examples/55_persistence_condition_skip_demo.yaml`: persistence-backed skipped-step inspection
- `examples/56_change_window_release_demo.yaml`: release change-window approval and packaged handoff
- `examples/57_incident_evidence_bundle_demo.yaml`: incident evidence collection and archive bundle
- `examples/58_runtime_expression_function_showcase.yaml`: focused runtime operator/function walkthrough
- `examples/59_branching_operator_showcase.yaml`: focused branching and iteration walkthrough
- `examples/60_template_branching_release_pack_demo.yaml`: template-driven branching, gating, and packaged artifact walkthrough
- `examples/61_template_wait_resume_release_pack_demo.yaml`: template + wait/resume + branching + packaged approval handoff walkthrough
- `examples/62_template_multi_stage_promotion_demo.yaml`: multi-stage promotion flow with template branching and approval handoff packaging
- `examples/63_null_semantics_showcase.yaml`: focused null, empty-string, and literal `"null"` semantics
- `examples/64_template_null_override_demo.yaml`: template override semantics for null-bearing parameter values
- `examples/65_persisted_null_resume_demo.yaml`: persisted null round-trip across wait/resume
- `examples/66_retry_parity_demo.yaml`: retry parity between persisted and non-persisted execution
- `examples/67_timeout_parity_demo.yaml`: timeout parity between persisted and non-persisted execution
- `examples/68_continue_on_error_parity_demo.yaml`: continue-on-error parity between persisted and non-persisted execution
- `examples/69_max_parallelism_parity_demo.yaml`: bounded parallelism parity between persisted and non-persisted execution
- `examples/70_wait_resume_parity_demo.yaml`: persisted wait/resume parity and payload capture
- `examples/71_callback_resume_identity_demo.yaml`: callback resume basics through the embedding host API
- `examples/72_callback_resume_two_cycle_demo.yaml`: repeated callback-driven resume cycles
- `examples/73_callback_resume_snapshot_safety_demo.yaml`: persisted workflow snapshot safety during callback resume
- `examples/74_control_flow_array_iteration_demo.yaml`: array-only iteration with runtime region gating
- `examples/75_mixed_template_runtime_control_flow_demo.yaml`: template-time branching plus runtime hotfix gating with structured metadata
- `examples/76_each_object_iteration_validation_error.yaml`: explicit unsupported object-target `${{ each }}` example
- `examples/77_template_null_condition_audit_demo.yaml`: template composition for null overrides, runtime gating, and audit-style output
- `examples/78_template_persisted_resume_observability_demo.yaml`: template-based persisted wait/resume composition with resumable summary output
- `examples/79_template_artifact_bundle_composition_demo.yaml`: template composition for manifest creation, gated region notes, and packaged output
- `examples/80_release_train_canary_approval.yaml`: release-train scenario with canary approval gating and packaged release evidence
- `examples/81_release_train_recovery_demo.yaml`: release recovery scenario with rollback packaging after a rejected canary
- `examples/82_incident_triage_severity_branching.yaml`: incident triage scenario with severity-based branching and evidence packaging
- `examples/83_maintenance_window_runbook_demo.yaml`: maintenance window scenario with wait/resume gating and packaged runbook handoff
- `examples/84_etl_reconciliation_audit_demo.yaml`: ETL reconciliation scenario with mismatch evidence and packaged handoff output
- `examples/85_compliance_audit_bundle_demo.yaml`: compliance audit scenario with control evidence and null-aware exception handling
- `examples/86_model_promotion_governance_demo.yaml`: model-promotion scenario with governance wait/resume and regional rollout evidence
- `examples/Procedo.Example.CustomSteps`: custom step registration patterns
- `examples/Procedo.Example.CallbackResumeHost`: host-level waiting-run query plus resume-by-wait-identity with configurable wait identity and payload inputs
- `examples/Procedo.Example.CustomResolverStore`: custom run-state-store and workflow-resolver wrapper example using public interfaces and callback resume
- `examples/Procedo.Example.DependencyInjection`: DI-based embedding
- `examples/Procedo.Example.AdvancedObservability`: persisted host flow with console plus JSONL event sinks and configurable resumable workflow inputs
- `examples/Procedo.Example.ParityRunner`: host-level persisted vs non-persisted parity comparison with configurable workflow selection
- `examples/Procedo.Example.PolicyHost`: host-level execution, validation, and system plugin security policy configuration
- `examples/Procedo.Example.WaitResume`: end-to-end wait/resume host flow

Run examples (all projects have built-in default YAMLs, so args are optional):

```powershell
# direct engine usage
dotnet run --project examples/Procedo.Example.Basic

# callback-driven resume host API
dotnet run --project examples/Procedo.Example.CallbackResumeHost

# custom store + custom workflow resolver wrapper
dotnet run --project examples/Procedo.Example.CustomResolverStore

# builder + extension pattern
dotnet run --project examples/Procedo.Example.Extensible

# delegate registration + DI activation + method binding
# all demonstrated in one example app
dotnet run --project examples/Procedo.Example.CustomSteps

# runtime conditions plus branching/iteration showcase
dotnet run --project examples/Procedo.Example.ControlFlow

# advanced observability with persisted resume and JSONL output
dotnet run --project examples/Procedo.Example.AdvancedObservability

# policy-driven host with locked-down system plugin options
dotnet run --project examples/Procedo.Example.PolicyHost

# Microsoft.Extensions.DependencyInjection integration
# demonstrated with Procedo.Extensions.DependencyInjection
dotnet run --project examples/Procedo.Example.DependencyInjection

# locked-down system plugin policy example
dotnet run --project examples/Procedo.Example.SecureRuntime

# generic wait -> resume signal walkthrough
dotnet run --project examples/Procedo.Example.WaitResume

# wait/resume with JSONL observability traces
dotnet run --project examples/Procedo.Example.WaitResumeObservability

# template + parameters + workflow variables via runtime CLI
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus

# richer parameter schema validation
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3

# runtime condition gating
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=dev

# template-time branching plus runtime conditions
dotnet run --project src/Procedo.Runtime -- examples/54_template_runtime_condition_demo.yaml

# persisted run with a skipped step you can inspect afterward
dotnet run --project src/Procedo.Runtime -- examples/55_persistence_condition_skip_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --show-run <runId> --state-dir .procedo/runs

# realistic change-window release that waits for approval
dotnet run --project src/Procedo.Runtime -- examples/56_change_window_release_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/56_change_window_release_demo.yaml --resume <runId> --resume-signal approve --state-dir .procedo/runs

# incident-response evidence bundle
dotnet run --project src/Procedo.Runtime -- examples/57_incident_evidence_bundle_demo.yaml

# focused runtime condition and expression-function showcase
dotnet run --project src/Procedo.Runtime -- examples/58_runtime_expression_function_showcase.yaml

# focused branching and each showcase
dotnet run --project src/Procedo.Runtime -- examples/59_branching_operator_showcase.yaml

# template-driven branching + region gating + packaged release bundle
dotnet run --project src/Procedo.Runtime -- examples/60_template_branching_release_pack_demo.yaml

# template-driven wait/resume approval flow
dotnet run --project src/Procedo.Runtime -- examples/61_template_wait_resume_release_pack_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/61_template_wait_resume_release_pack_demo.yaml --resume <runId> --resume-signal approve --state-dir .procedo/runs

# multi-stage promotion flow with approval pause
dotnet run --project src/Procedo.Runtime -- examples/62_template_multi_stage_promotion_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/62_template_multi_stage_promotion_demo.yaml --resume <runId> --resume-signal approve --state-dir .procedo/runs

# null semantics walkthrough
dotnet run --project src/Procedo.Runtime -- examples/63_null_semantics_showcase.yaml

# template null-override walkthrough
dotnet run --project src/Procedo.Runtime -- examples/64_template_null_override_demo.yaml

# persisted null round-trip walkthrough
dotnet run --project src/Procedo.Runtime -- examples/65_persisted_null_resume_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/65_persisted_null_resume_demo.yaml --resume <runId> --resume-signal continue --state-dir .procedo/runs

# retry parity walkthrough
dotnet run --project src/Procedo.Runtime -- examples/66_retry_parity_demo.yaml
dotnet run --project src/Procedo.Runtime -- examples/66_retry_parity_demo.yaml --persist --state-dir .procedo/runs

# timeout parity walkthrough
dotnet run --project src/Procedo.Runtime -- examples/67_timeout_parity_demo.yaml
dotnet run --project src/Procedo.Runtime -- examples/67_timeout_parity_demo.yaml --persist --state-dir .procedo/runs

# continue-on-error parity walkthrough
dotnet run --project src/Procedo.Runtime -- examples/68_continue_on_error_parity_demo.yaml
dotnet run --project src/Procedo.Runtime -- examples/68_continue_on_error_parity_demo.yaml --persist --state-dir .procedo/runs

# bounded parallelism parity walkthrough
dotnet run --project src/Procedo.Runtime -- examples/69_max_parallelism_parity_demo.yaml
dotnet run --project src/Procedo.Runtime -- examples/69_max_parallelism_parity_demo.yaml --persist --state-dir .procedo/runs

# wait/resume parity walkthrough
dotnet run --project src/Procedo.Runtime -- examples/70_wait_resume_parity_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/70_wait_resume_parity_demo.yaml --resume <runId> --resume-signal continue --resume-payload-json '{"ticket":"CHG-700"}' --state-dir .procedo/runs

# callback resume examples are host-API scenarios rather than CLI-only scenarios
# they are exercised by WorkflowCallbackResumeIntegrationTests
# first run still uses the normal waiting CLI path if you want to inspect persisted state manually
dotnet run --project src/Procedo.Runtime -- examples/71_callback_resume_identity_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/72_callback_resume_two_cycle_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/73_callback_resume_snapshot_safety_demo.yaml --persist --state-dir .procedo/runs

# structured-array each plus runtime gating
dotnet run --project src/Procedo.Runtime -- examples/74_control_flow_array_iteration_demo.yaml

# template-time branching plus runtime gating and hotfix smoke targeting
dotnet run --project src/Procedo.Runtime -- examples/75_mixed_template_runtime_control_flow_demo.yaml

# unsupported object-target each example (expected failure)
dotnet run --project src/Procedo.Runtime -- examples/76_each_object_iteration_validation_error.yaml

# medium-complexity template composition with null overrides and runtime gating
dotnet run --project src/Procedo.Runtime -- examples/77_template_null_condition_audit_demo.yaml

# template-driven persisted resume composition
dotnet run --project src/Procedo.Runtime -- examples/78_template_persisted_resume_observability_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/78_template_persisted_resume_observability_demo.yaml --resume <runId> --resume-signal approve --resume-payload-json '{"ticket":"CHG-780","approved_by":"ops-bot"}' --state-dir .procedo/runs

# composed template bundle with packaged artifacts
dotnet run --project src/Procedo.Runtime -- examples/79_template_artifact_bundle_composition_demo.yaml

# release-train approval scenario
dotnet run --project src/Procedo.Runtime -- examples/80_release_train_canary_approval.yaml --persist --state-dir .procedo/release-train-canary-cli
dotnet run --project src/Procedo.Runtime -- examples/80_release_train_canary_approval.yaml --resume <runId> --resume-signal approve --state-dir .procedo/release-train-canary-cli

# release-train recovery scenario
dotnet run --project src/Procedo.Runtime -- examples/81_release_train_recovery_demo.yaml

# incident triage severity branching scenario
dotnet run --project src/Procedo.Runtime -- examples/82_incident_triage_severity_branching.yaml

# maintenance window runbook scenario
dotnet run --project src/Procedo.Runtime -- examples/83_maintenance_window_runbook_demo.yaml --persist --state-dir .procedo/maintenance-window-cli
dotnet run --project src/Procedo.Runtime -- examples/83_maintenance_window_runbook_demo.yaml --resume <runId> --resume-signal start --state-dir .procedo/maintenance-window-cli

# ETL reconciliation audit scenario
dotnet run --project src/Procedo.Runtime -- examples/84_etl_reconciliation_audit_demo.yaml

# compliance audit bundle scenario
dotnet run --project src/Procedo.Runtime -- examples/85_compliance_audit_bundle_demo.yaml

# model-promotion governance scenario
dotnet run --project src/Procedo.Runtime -- examples/86_model_promotion_governance_demo.yaml --persist --state-dir .procedo/model-promotion-cli
dotnet run --project src/Procedo.Runtime -- examples/86_model_promotion_governance_demo.yaml --resume <runId> --resume-signal approve --state-dir .procedo/model-promotion-cli

# comprehensive template-driven release bundle
dotnet run --project src/Procedo.Runtime -- examples/50_comprehensive_template_release_demo.yaml

# comprehensive built-in system bundle flow
dotnet run --project src/Procedo.Runtime -- examples/51_comprehensive_system_bundle_demo.yaml

# comprehensive wait/resume bundle flow
# first run waits for approved.txt; create the file and resume with a check signal
dotnet run --project src/Procedo.Runtime -- examples/52_comprehensive_wait_resume_bundle_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/52_comprehensive_wait_resume_bundle_demo.yaml --resume <runId> --resume-signal check --state-dir .procedo/runs

# embedded host example for template execution
dotnet run --project examples/Procedo.Example.Templates

# template-driven release bundle example app
dotnet run --project examples/Procedo.Example.TemplateReleasePack

# template-driven wait/resume example app
dotnet run --project examples/Procedo.Example.TemplateWaitResume

# multi-stage promotion example app
dotnet run --project examples/Procedo.Example.MultiStagePromotion

# runtime CLI wait-file example
# first run will wait; after creating the file, resume with a check signal
dotnet run --project src/Procedo.Runtime -- examples/47_wait_file_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/47_wait_file_demo.yaml --resume <runId> --resume-signal check --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --list-waiting --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --delete-run <runId> --state-dir .procedo/runs

# validation-focused walkthrough
dotnet run --project examples/Procedo.Example.Validation

# persistence + resume walkthrough
dotnet run --project examples/Procedo.Example.PersistenceResume

# persisted vs non-persisted host parity comparison
dotnet run --project examples/Procedo.Example.ParityRunner
dotnet run --project examples/Procedo.Example.ParityRunner -- --workflow examples/69_max_parallelism_parity_demo.yaml

# callback-resume host with explicit wait identity and payload
dotnet run --project examples/Procedo.Example.CallbackResumeHost -- --workflow examples/71_callback_resume_identity_demo.yaml --wait-key callback-identity-demo --expected-signal approve --signal-type approve --payload-json '{"approved_by":"ops-bot","ticket":"CHG-710"}'

# custom store + resolver wrapper with explicit wait identity
dotnet run --project examples/Procedo.Example.CustomResolverStore -- --workflow examples/71_callback_resume_identity_demo.yaml --wait-key callback-identity-demo --signal-type approve

# advanced observability host with explicit resumable workflow inputs
dotnet run --project examples/Procedo.Example.AdvancedObservability -- --workflow examples/78_template_persisted_resume_observability_demo.yaml --resume-signal approve --resume-payload-json '{"ticket":"CHG-780","approved_by":"ops-observer"}'

# policy-driven host with explicit artifacts directory
dotnet run --project examples/Procedo.Example.PolicyHost -- --artifacts-dir .procedo/custom-policy-host

# observability (console + jsonl sink)
dotnet run --project examples/Procedo.Example.Observability

# simple-to-complex scenario pack
dotnet run --project examples/Procedo.Example.ScenarioPack

# umbrella launcher for example projects and catalogs
dotnet run --project examples/Procedo.Example.Catalog -- --list
dotnet run --project examples/Procedo.Example.Catalog -- --run basic
dotnet run --project examples/Procedo.Example.Catalog -- --run catalogs
dotnet run --project examples/Procedo.Example.Catalog -- --run all

# foundational yaml catalog suite
dotnet run --project examples/Procedo.Example.Catalog.Foundation

# resilience yaml catalog suite
dotnet run --project examples/Procedo.Example.Catalog.Resilience

# enterprise yaml catalog suite
dotnet run --project examples/Procedo.Example.Catalog.Enterprise

# targeted single-yaml run with CLI host
dotnet run --project src/Procedo.Runtime -- examples/40_system_process_demo.yaml
```

## Expected outcomes for demo examples

- `09_retry_transient.yaml`: expected run success (flaky step succeeds after retries)
- `10_timeout_failure.yaml`: expected run failure (step timeout)
- `11_continue_on_error_false.yaml`: expected run failure (fail-fast)
- `12_continue_on_error_true.yaml`: expected run failure after running sibling step
- `17_persistence_resume_after_failure.yaml`: expected first run failure, resume run success
- `21_cancellation_demo.yaml`: expected run failure
- `24_end_to_end_reference.yaml`: expected run success
- `25_data_platform_full_pipeline.yaml`: expected run success
- `26_branched_release_train.yaml`: expected run failure (security branch has intentional failure)
- `28_ml_feature_pipeline.yaml`: expected run success
- `29_finops_daily_close.yaml`: expected run failure with continue-on-error behavior
- `30_enterprise_reference_pipeline.yaml`: expected validation failure (cross-job dependencies under current validator rules)
- `40_system_process_demo.yaml`: expected run success on machines with `dotnet` available on `PATH`
- `41_custom_steps_inline_demo.yaml`: expected run success using delegate registration, DI-backed activation, and method binding
- `42_dependency_injection_demo.yaml`: expected run success using `IServiceCollection`-based setup
- `43_secure_runtime_allowed.yaml`: expected run success inside a locked-down host with allowed file output
- `44_secure_runtime_blocked_process.yaml`: expected run failure inside a locked-down host because process execution is disabled
- `45_wait_signal_demo.yaml`: expected first run to enter waiting state; resume with `continue` succeeds
- `46_wait_resume_observability.yaml`: expected first run to enter waiting state and emit structured events; resume succeeds
- `47_wait_file_demo.yaml`: expected first run to enter waiting state until the target file exists; resume/check succeeds after file creation
- `48_template_parameters_demo.yaml`: expected run success when required runtime parameters are supplied
- `49_parameter_schema_validation_demo.yaml`: expected run success when provided values satisfy the declared schema
- `50_comprehensive_template_release_demo.yaml`: expected run success and bundle creation using template defaults plus child overrides
- `51_comprehensive_system_bundle_demo.yaml`: expected run success with generated archive, extracted files, and bundle hash
- `52_comprehensive_wait_resume_bundle_demo.yaml`: expected first run to enter waiting state; after creating `approved.txt`, resume succeeds and creates an approval bundle
- `53_runtime_condition_demo.yaml`: expected run success with `deploy_prod` skipped and non-prod steps completed by default
- `54_template_runtime_condition_demo.yaml`: expected run success using a template-expanded production rollout
- `55_persistence_condition_skip_demo.yaml`: expected run success with `gated_prod_only` persisted as skipped
- `56_change_window_release_demo.yaml`: expected first run to enter waiting state; resume with `approve` creates a release bundle
- `57_incident_evidence_bundle_demo.yaml`: expected run success with a generated incident evidence archive and expanded output
- `58_runtime_expression_function_showcase.yaml`: expected run success with `prod_only_gate` skipped and the operator/function showcase steps completed
- `59_branching_operator_showcase.yaml`: expected run success with the QA branch expanded, `deploy_eastus` skipped, and west/central region steps completed
- `60_template_branching_release_pack_demo.yaml`: expected run success with `branch_prod`, west/central rollout, a skipped east region, and a generated release pack zip
- `61_template_wait_resume_release_pack_demo.yaml`: expected first run to enter waiting state; resume with `approve` completes packaging and writes the approval bundle zip
- `62_template_multi_stage_promotion_demo.yaml`: expected first run to enter waiting state; resume with `approve` completes the multi-stage promotion bundle
- `63_null_semantics_showcase.yaml`: expected run success with structured output preserving real `null`, empty string, and literal `"null"` distinctly
- `64_template_null_override_demo.yaml`: expected run success with template overrides preserving null values instead of coercing them
- `65_persisted_null_resume_demo.yaml`: expected first run to enter waiting state; resume with `continue` preserves null-bearing parameters through persisted run state and output generation
- `66_retry_parity_demo.yaml`: expected run success in both persisted and non-persisted modes with a matching retry summary artifact
- `67_timeout_parity_demo.yaml`: expected run failure in both persisted and non-persisted modes with timeout error semantics
- `68_continue_on_error_parity_demo.yaml`: expected run failure in both persisted and non-persisted modes while still writing the sibling-work artifact
- `69_max_parallelism_parity_demo.yaml`: expected run success in both persisted and non-persisted modes with a bounded two-wide parallel batch
- `70_wait_resume_parity_demo.yaml`: expected first run to enter waiting state; resume with `continue` writes the signal type and payload to the parity artifact
- `71_callback_resume_identity_demo.yaml`: expected first run to enter waiting state; dedicated host-backed verification resumes it by wait identity and writes the callback payload
- `72_callback_resume_two_cycle_demo.yaml`: expected first run to enter waiting state; dedicated host-backed verification resumes two wait cycles by identity in sequence
- `73_callback_resume_snapshot_safety_demo.yaml`: expected first run to enter waiting state; dedicated host-backed verification changes the YAML file and still resumes against the persisted snapshot safely
- `74_control_flow_array_iteration_demo.yaml`: expected run success with the QA branch expanded, eastus gated off, and west/central region rollout steps completed
- `75_mixed_template_runtime_control_flow_demo.yaml`: expected run success with the production branch expanded, eastus gated off, only central hotfix verification executed, and a structured summary file written
- `76_each_object_iteration_validation_error.yaml`: expected load/validation failure because `${{ each }}` rejects object and dictionary targets
- `77_template_null_condition_audit_demo.yaml`: expected run success with the QA branch selected, eastus gated off, null metadata preserved, and a JSON audit summary written
- `78_template_persisted_resume_observability_demo.yaml`: expected first run to enter waiting state; resume with `approve` writes a resumed summary artifact with payload and metadata
- `79_template_artifact_bundle_composition_demo.yaml`: expected run success with eastus gated off, west/central notes written, a composition bundle zip created, and a SHA256 hash file written
- `80_release_train_canary_approval.yaml`: expected first run to enter waiting state; resume with `approve` creates the release bundle, writes rollout notes, and records canary approval artifacts
- `81_release_train_recovery_demo.yaml`: expected run success with rejected canary handling, a rollback bundle zip, and a recovery manifest written
- `82_incident_triage_severity_branching.yaml`: expected run success with severity-specific branch selection, containment or triage notes, and an incident evidence bundle
- `83_maintenance_window_runbook_demo.yaml`: expected first run to enter waiting state; resume with `start` creates the maintenance bundle and writes checklist/receipt artifacts
- `84_etl_reconciliation_audit_demo.yaml`: expected run success with mismatch reporting, reconciliation evidence, a packaged ETL bundle, and a persisted hash file
- `85_compliance_audit_bundle_demo.yaml`: expected run success with control checklist evidence, a no-exception note by default, a packaged audit bundle, and a persisted hash file
- `86_model_promotion_governance_demo.yaml`: expected first run to enter waiting state; resume with `approve` writes west/central rollout notes, keeps eastus gated, and creates a packaged promotion bundle with a persisted hash file

## Useful runtime commands

```powershell
# basic run
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml

# strict validation
dotnet run --project src/Procedo.Runtime -- examples/13_missing_plugin_validation_error.yaml --strict-validation

# persistence + resume
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/runs
```

## Note on runtime project

`src/Procedo.Runtime` is the reference CLI host (local execution entrypoint), not a UI.

End users embedding Procedo in their own apps should use engine/library packages (`Procedo.*`) and can follow the example consumer projects above.


