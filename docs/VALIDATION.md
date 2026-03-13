# Validation

Procedo validates workflows before execution.

For most users, validation is part of the `Procedo.Hosting` experience: parse the workflow, build a host, and let the host validate before execution. Manual validation is still available when you want explicit control.

## Validator

`ProcedoWorkflowValidator` checks structural and semantic rules, including:

- required fields
- typed parameter compatibility
- parameter default/value constraint enforcement
- duplicate stage/job/step ids
- `depends_on` references exist
- dependency cycles
- step type format (`namespace.operation`)
- plugin type resolvability
- expression reference safety
- runtime `condition:` reference safety

Parameter schema validation now supports:

- `allowed_values`
- numeric `min` / `max`
- string `min_length` / `max_length` / `pattern`
- array `item_type`
- object `required_properties`

Example:

```yaml
parameters:
  environment:
    type: string
    allowed_values:
    - dev
    - prod
    min_length: 3
    pattern: "^[a-z]+$"

  retry_count:
    type: int
    min: 1
    max: 5

  targets:
    type: array
    item_type: string

  metadata:
    type: object
    required_properties:
    - team
```

Flow/control authoring validation also covers:

- malformed `${{ if }}` / `${{ elseif }}` / `${{ else }}`
- malformed `${{ each item in collection }}`
- unsupported expression functions or bad argument counts
- runtime `condition:` references to unknown `params.*`, `vars.*`, or step outputs
- runtime `condition:` references to step outputs without a matching dependency chain

Runtime condition example:

```yaml
- step: deploy_prod
  type: system.echo
  depends_on:
  - build
  condition: and(eq(params.environment, 'prod'), eq(steps.build.outputs.ready, true))
  with:
    message: "Deploying ${params.environment}"
```

## Expression and directive failures

Procedo validates both template-time control-flow expressions and runtime `condition:` expressions.

Common authoring failures include:

- unknown function names such as `equal(...)` instead of `eq(...)`
- wrong argument counts such as `not(a, b)` or `eq(a)`
- references to unknown `params.*`, `vars.*`, or step outputs
- `${{ each }}` expressions that do not evaluate to an array
- runtime `condition:` expressions that do not evaluate to a boolean

Typical outcomes:

- template-time directive problems fail during parse/load/validation before execution starts
- runtime `condition:` reference problems are surfaced during validation when references can be analyzed
- runtime `condition:` type/evaluation problems fail the affected step at execution time if the expression cannot be evaluated as a boolean

Examples:

Invalid function name:

```yaml
condition: equal(params.environment, 'prod')
```

Invalid boolean condition:

```yaml
condition: format('{0}-{1}', params.service_name, params.environment)
```

Invalid `${{ each }}` target:

```yaml
${{ each region in params.primary_region }}:
- step: deploy_${region}
  type: system.echo
```

Use these examples when debugging authoring issues:

- [Control-Flow Recipes](/D:/Project/codenjwu/Procedo/docs/CONTROL_FLOW_RECIPES.md)
- [58_runtime_expression_function_showcase.yaml](/D:/Project/codenjwu/Procedo/examples/58_runtime_expression_function_showcase.yaml)
- [59_branching_operator_showcase.yaml](/D:/Project/codenjwu/Procedo/examples/59_branching_operator_showcase.yaml)
- [60_template_branching_release_pack_demo.yaml](/D:/Project/codenjwu/Procedo/examples/60_template_branching_release_pack_demo.yaml)

## Modes

- Permissive: warnings may be emitted and execution can continue.
- Strict (`--strict-validation`): elevated checks can fail execution.

Runtime example:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --strict-validation
```

If validation has errors, runtime exits non-zero and does not execute workflow steps.

Even if validation is skipped, Procedo still enforces parameter coercion and schema constraints during parameter resolution.

## Why strict mode exists

Strict mode is useful for CI and production-hardening where ambiguous or risky workflow definitions should fail fast.
