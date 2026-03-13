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
- `examples/Procedo.Example.CustomSteps`: custom step registration patterns
- `examples/Procedo.Example.DependencyInjection`: DI-based embedding
- `examples/Procedo.Example.WaitResume`: end-to-end wait/resume host flow

Run examples (all projects have built-in default YAMLs, so args are optional):

```powershell
# direct engine usage
dotnet run --project examples/Procedo.Example.Basic

# builder + extension pattern
dotnet run --project examples/Procedo.Example.Extensible

# delegate registration + DI activation + method binding
# all demonstrated in one example app
dotnet run --project examples/Procedo.Example.CustomSteps

# runtime conditions plus branching/iteration showcase
dotnet run --project examples/Procedo.Example.ControlFlow

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


