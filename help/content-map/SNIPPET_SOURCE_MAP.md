# Snippet Source Map

This file lists the strongest initial snippet sources for the launch pages.

## Phase 1 Sources

| Page | Primary Source |
|---|---|
| Run Your First Workflow | `examples/01_hello_echo.yaml` |
| Create Your First Workflow | `examples/01_hello_echo.yaml`, `examples/hello_pipeline.yaml` |
| Parameters | `examples/48_template_parameters_demo.yaml`, `examples/49_parameter_schema_validation_demo.yaml` |
| Outputs | `examples/05_outputs_and_expressions.yaml` |
| Conditions | `examples/53_runtime_condition_demo.yaml`, `examples/58_runtime_expression_function_showcase.yaml` |
| Persistence | `examples/16_persistence_resume_happy_path.yaml`, `examples/55_persistence_condition_skip_demo.yaml` |
| Observability | `examples/18_observability_console_events.yaml`, `examples/19_observability_jsonl_events.yaml` |
| Validation | `examples/49_parameter_schema_validation_demo.yaml`, `examples/13_missing_plugin_validation_error.yaml`, `examples/14_cycle_dependency_validation_error.yaml` |
| Minimal Pipeline | `examples/01_hello_echo.yaml` |
| Passing Data Between Steps | `examples/05_outputs_and_expressions.yaml` |
| Conditional Execution | `examples/53_runtime_condition_demo.yaml` |

## Rules

- Prefer full example files for launch content.
- Only add fragment snippets when a full example would be too long for the page.
- Fragment snippets should later be linked back to their source example or a validated snippet fixture.
