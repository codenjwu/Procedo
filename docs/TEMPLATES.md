# Templates

Procedo supports a narrow, predictable template model for Phase 1.

This first version is designed for:

- reusable base workflows
- parameterized environment or deployment differences
- workflow-level variable customization
- runtime parameter overrides from hosts or the CLI

It is intentionally not a full inheritance or fragment-composition system.

Procedo now has two distinct evaluation phases:

- template-time expansion using `${{ if }}`, `${{ elseif }}`, `${{ else }}`, and array-only `${{ each }}`
- runtime step gating using `condition: <expression>`

## Supported concepts

### 1. Parameters

Parameters define values that are supplied to a workflow from a child template consumer, host application, or CLI.

Example:

```yaml
parameters:
  environment:
    type: string
    required: true

  region:
    type: string
    default: eastus
```

Supported parameter types in this first version:

- `string`
- `int` / `integer`
- `bool` / `boolean`
- `number`
- `object`
- `array`

Parameter definitions may also declare lightweight validation constraints such as:

- `allowed_values`
- `min` / `max`
- `min_length` / `max_length`
- `pattern`
- `item_type`
- `required_properties`

## 2. Workflow variables

Workflow variables are resolved before step execution and may reference:

- `${params.<name>}`
- `${vars.<name>}`

Example:

```yaml
variables:
  artifact_name: "${params.service_name}-${params.environment}-${params.region}"
```

Workflow variables may not reference step outputs.

## 3. Templates

A workflow may reference one base template file.

Example child workflow:

```yaml
template: ./templates/standard_build_template.yaml
name: template_parameters_demo

parameters:
  service_name: procedo

variables:
  artifact_name: "custom-${params.service_name}-${params.environment}"
```

Base template example:

```yaml
name: standard_build_template
version: 1

parameters:
  service_name:
    type: string
    required: true
  environment:
    type: string
    default: dev
  region:
    type: string
    default: eastus

variables:
  artifact_name: "${params.service_name}-${params.environment}-${params.region}"

stages:
- stage: build
  jobs:
  - job: package
    steps:
    - step: announce
      type: system.echo
      with:
        message: "Building ${vars.artifact_name}"
```

Template-time flow/control example:

```yaml
parameters:
  environment: prod
  regions:
  - eastus
  - westus

stages:
- stage: deploy
  jobs:
  - job: main
    steps:
      ${{ if eq(params.environment, 'prod') }}:
      - step: prod_banner
        type: system.echo
        with:
          message: "Production deployment"
      ${{ each region in params.regions }}:
      - step: deploy_${region}
        type: system.echo
        with:
          message: "Deploying ${region}"
```

Null-valued parameters, variables, and `with:` values are preserved as `null` through parsing and template merge. Bare YAML `null` and `~` are treated as null values, while quoted `"null"` remains the literal string.

## Merge rules

In this first implementation, the child workflow may override:

- parameter values
- workflow variables
- top-level execution settings like `max_parallelism` and `continue_on_error`
- workflow `name`

The child workflow may not define:

- new stages
- new jobs
- new steps
- new parameter schema definitions

If a child workflow tries to define stages or new parameter schemas, loading fails.

## Runtime usage

### CLI

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus

# structured CLI values via inline JSON
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param metadata={"team":"platform","priority":2} --param targets=["eastus","westus"]

# structured CLI values via JSON file
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param deployment=@./deployment.parameters.json
```

### Embedded host

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .Build();

var result = await host.ExecuteFileAsync(
    "examples/48_template_parameters_demo.yaml",
    new Dictionary<string, object>
    {
        ["environment"] = "prod",
        ["region"] = "westus"
    });
```

## Error reporting

Template loader errors now include the source workflow and template file path when available.

Examples:

- `Workflow ''D:\repo\child.yaml'' cannot define stages when using template ''D:\repo\templates\base.yaml''.`
- `Template cycle detected while loading ''D:\repo\templates\base.yaml''.`
- inline template loading uses the source label `<inline>` when the workflow did not originate from a file path

Validation issues produced from expanded workflows now carry a best-effort `SourcePath` too. In the CLI host, that file path is included in validation output when available. Runtime execution failures now surface the expanded source file path on the run result, `RunFailed` event, and `StepFailed` event when the failing graph or step came from a template. This is still a lightweight attribution model, not a full future-proof source map for arbitrary composition features.

Runtime `condition:` is evaluated after template expansion, using the resolved runtime context. In practice:

- use template-time directives when you want to add or remove YAML nodes
- use `condition:` when you want a declared step to be skipped at runtime

Current nuance: template-time directives inside a base template evaluate against values visible while that template is parsed. Child workflow parameter overrides are merged later, so use runtime `condition:` when a branch must react to child-supplied values reliably in Phase 1.

Additional Phase 1 control-flow nuances:

- `${{ each }}` is array-only
- `${{ each }}` rejects object and dictionary targets instead of iterating them implicitly
- use [64_template_null_override_demo.yaml](../examples/64_template_null_override_demo.yaml) when you want a concrete null-override example for template consumers

## Validation behavior

Procedo validates:

- missing required parameters
- undeclared supplied parameters when a schema exists
- incompatible parameter values
- unknown `${params.*}` references
- unknown `${vars.*}` references
- workflow variable self-reference and cyclic references
- invalid template cycles at load time

## Current limitations

Phase 1 template support does not yet include:

- arbitrary stage/job/step merging
- template fragment imports
- multiple template inheritance
- full source mapping for hypothetical future graph-composition features
- object/dictionary iteration for `${{ each }}`
- base-template `${{ if }}` / `${{ each }}` blocks do not yet re-evaluate against child override values
- shell-specific escaping rules still apply for inline JSON values
- `@path` is required when you want a CLI parameter value loaded from a JSON file

## Recommended authoring pattern

Use templates when the execution graph is stable and only values differ.

Good fit:

- environment-specific deployment workflows
- repeated build/package/publish flows
- standard integration patterns with different service names or regions

Not a good fit yet:

- workflows that need to inject or remove steps dynamically
- heavy graph composition across many partial files

## Reference files

- [48_template_parameters_demo.yaml](../examples/48_template_parameters_demo.yaml)
- [60_template_branching_release_pack_demo.yaml](../examples/60_template_branching_release_pack_demo.yaml)
- [61_template_wait_resume_release_pack_demo.yaml](../examples/61_template_wait_resume_release_pack_demo.yaml)
- [62_template_multi_stage_promotion_demo.yaml](../examples/62_template_multi_stage_promotion_demo.yaml)
- [64_template_null_override_demo.yaml](../examples/64_template_null_override_demo.yaml)
- [77_template_null_condition_audit_demo.yaml](../examples/77_template_null_condition_audit_demo.yaml)
- [78_template_persisted_resume_observability_demo.yaml](../examples/78_template_persisted_resume_observability_demo.yaml)
- [79_template_artifact_bundle_composition_demo.yaml](../examples/79_template_artifact_bundle_composition_demo.yaml)
- [null_semantics_template.yaml](../examples/templates/null_semantics_template.yaml)
- [composed_audit_template.yaml](../examples/templates/composed_audit_template.yaml)
- [composed_resume_observability_template.yaml](../examples/templates/composed_resume_observability_template.yaml)
- [composed_artifact_bundle_template.yaml](../examples/templates/composed_artifact_bundle_template.yaml)
- [standard_build_template.yaml](../examples/templates/standard_build_template.yaml)
- [complex_branching_release_pack_template.yaml](../examples/templates/complex_branching_release_pack_template.yaml)
- [complex_branching_wait_resume_release_template.yaml](../examples/templates/complex_branching_wait_resume_release_template.yaml)
- [multi_stage_promotion_template.yaml](../examples/templates/multi_stage_promotion_template.yaml)
- [Program.cs](../examples/Procedo.Example.Templates/Program.cs)
- [Control-Flow Recipes](./CONTROL_FLOW_RECIPES.md)

