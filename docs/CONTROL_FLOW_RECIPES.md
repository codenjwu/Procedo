# Control-Flow Recipes

This page is the shortest path to Procedo examples that demonstrate flow/control features in realistic combinations.

Procedo Phase 1 has two layers of flow control:

- template-time expansion with `${{ if }}`, `${{ elseif }}`, `${{ else }}`, and array-only `${{ each }}`
- runtime step gating with `condition: <expression>`

Use this page when you want a runnable example for a specific control-flow pattern.

## Quick map

- runtime `condition:` and expression functions:
  - [58_runtime_expression_function_showcase.yaml](/D:/Project/codenjwu/Procedo/examples/58_runtime_expression_function_showcase.yaml)
- focused branching and iteration:
  - [59_branching_operator_showcase.yaml](/D:/Project/codenjwu/Procedo/examples/59_branching_operator_showcase.yaml)
- array-only iteration plus runtime region gating:
  - [74_control_flow_array_iteration_demo.yaml](/D:/Project/codenjwu/Procedo/examples/74_control_flow_array_iteration_demo.yaml)
- mixed template-time branching plus runtime gating:
  - [75_mixed_template_runtime_control_flow_demo.yaml](/D:/Project/codenjwu/Procedo/examples/75_mixed_template_runtime_control_flow_demo.yaml)
- explicit unsupported object-target `${{ each }}`:
  - [76_each_object_iteration_validation_error.yaml](/D:/Project/codenjwu/Procedo/examples/76_each_object_iteration_validation_error.yaml)
- template + branching + artifact packaging:
  - [60_template_branching_release_pack_demo.yaml](/D:/Project/codenjwu/Procedo/examples/60_template_branching_release_pack_demo.yaml)
- template + branching + wait/resume:
  - [61_template_wait_resume_release_pack_demo.yaml](/D:/Project/codenjwu/Procedo/examples/61_template_wait_resume_release_pack_demo.yaml)
- multi-stage promotion flow:
  - [62_template_multi_stage_promotion_demo.yaml](/D:/Project/codenjwu/Procedo/examples/62_template_multi_stage_promotion_demo.yaml)

## Recipe 1: Runtime conditions and expression functions

Use [58_runtime_expression_function_showcase.yaml](/D:/Project/codenjwu/Procedo/examples/58_runtime_expression_function_showcase.yaml) when you want to learn:

- `or(...)`
- `not(...)`
- `endsWith(...)`
- `in(...)`
- `ne(...)`
- `contains(...)`
- `format(...)`

It is the cleanest example for “keep all steps declared, but skip some at runtime.”

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/58_runtime_expression_function_showcase.yaml
```

## Recipe 2: Branching and `${{ each }}`

Use [59_branching_operator_showcase.yaml](/D:/Project/codenjwu/Procedo/examples/59_branching_operator_showcase.yaml) when you want to see:

- `${{ if }}`
- `${{ elseif }}`
- `${{ else }}`
- `${{ each }}`
- runtime gating layered on top of expanded steps

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/59_branching_operator_showcase.yaml
```

## Recipe 3: Array iteration with runtime gating

Use [74_control_flow_array_iteration_demo.yaml](/D:/Project/codenjwu/Procedo/examples/74_control_flow_array_iteration_demo.yaml) when you want to see:

- `${{ each }}` over an array of scalar values
- expanded step ids derived from the loop item
- runtime `condition:` layered on top of expanded steps
- a clean example of “expand everything, then gate selected regions at runtime”

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/74_control_flow_array_iteration_demo.yaml
```

## Recipe 4: Mixed template-time branching and runtime gating

Use [75_mixed_template_runtime_control_flow_demo.yaml](/D:/Project/codenjwu/Procedo/examples/75_mixed_template_runtime_control_flow_demo.yaml) with [control_flow_mix_template.yaml](/D:/Project/codenjwu/Procedo/examples/templates/control_flow_mix_template.yaml) when you want:

- template inheritance
- template-time `${{ if }}` / `${{ elseif }}` / `${{ else }}`
- `${{ each }}` over rollout regions
- runtime gating for active regions and hotfix smoke regions
- structured metadata preserved into the final summary artifact
- a lighter-weight mixed example than the release-pack scenarios

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/75_mixed_template_runtime_control_flow_demo.yaml
```

## Recipe 5: Explicit unsupported object iteration

Use [76_each_object_iteration_validation_error.yaml](/D:/Project/codenjwu/Procedo/examples/76_each_object_iteration_validation_error.yaml) when you want the clearest example of what is not supported:

- `${{ each }}` rejects object and dictionary targets
- the target must evaluate to an array

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/76_each_object_iteration_validation_error.yaml
```

## Recipe 6: Template-driven release pack

Use [60_template_branching_release_pack_demo.yaml](/D:/Project/codenjwu/Procedo/examples/60_template_branching_release_pack_demo.yaml) with [complex_branching_release_pack_template.yaml](/D:/Project/codenjwu/Procedo/examples/templates/complex_branching_release_pack_template.yaml) when you want:

- template inheritance
- template-time branching
- region expansion with `${{ each }}`
- runtime region/channel gating
- real output files, zip packaging, and hashing

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/60_template_branching_release_pack_demo.yaml
```

## Recipe 7: Template-driven wait/resume operator flow

Use [61_template_wait_resume_release_pack_demo.yaml](/D:/Project/codenjwu/Procedo/examples/61_template_wait_resume_release_pack_demo.yaml) with [complex_branching_wait_resume_release_template.yaml](/D:/Project/codenjwu/Procedo/examples/templates/complex_branching_wait_resume_release_template.yaml) when you want:

- template branching
- runtime gating
- persisted `wait_signal`
- resume with `--resume-signal`
- approval receipt and handoff bundle generation

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/61_template_wait_resume_release_pack_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/61_template_wait_resume_release_pack_demo.yaml --resume <runId> --resume-signal approve --state-dir .procedo/runs
```

## Recipe 8: Multi-stage promotion workflow

Use [62_template_multi_stage_promotion_demo.yaml](/D:/Project/codenjwu/Procedo/examples/62_template_multi_stage_promotion_demo.yaml) with [multi_stage_promotion_template.yaml](/D:/Project/codenjwu/Procedo/examples/templates/multi_stage_promotion_template.yaml) when you want the most “operator-like” example in the repo:

- multiple stages and jobs
- template-selected routing
- regional rollout planning
- persisted approval before final packaging
- visible skipped-step behavior in the saved run

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/62_template_multi_stage_promotion_demo.yaml --persist --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- examples/62_template_multi_stage_promotion_demo.yaml --resume <runId> --resume-signal approve --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --show-run <runId> --state-dir .procedo/runs
```

## Authoring guidance

Use template-time directives when you need to shape the workflow graph:

- add or remove steps
- choose one branch of YAML
- expand repeated steps from an array

Use runtime `condition:` when you want a declared step to remain in the graph but execute only when the runtime context matches.

Phase 1 nuances:

- `${{ each }}` is array-only
- `${{ each }}` rejects object and dictionary targets
- base-template directives do not re-evaluate against child override values after merge
- runtime `condition:` is the reliable tool when child-supplied values must affect execution behavior

Related docs:

- [Templates](/D:/Project/codenjwu/Procedo/docs/TEMPLATES.md)
- [Validation](/D:/Project/codenjwu/Procedo/docs/VALIDATION.md)
- [Examples Catalog](/D:/Project/codenjwu/Procedo/examples/README.md)
