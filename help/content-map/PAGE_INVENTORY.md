# Procedo Help Page Inventory

This inventory maps planned help pages to current repository sources and identifies where new writing or new examples are still needed.

## Overview

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Introduction | Concept | `README.md` | Rewrite for user-facing positioning and use cases |
| Why Procedo | Concept | `README.md` | Add stronger value framing, tradeoffs, and scenarios |
| Core Concepts | Concept | `README.md`, `src/Procedo.Core/Models/` | New user-focused explanation |
| Feature Map | Concept | `README.md`, `docs/` | New content |
| Package Overview | Reference | `docs/PACKAGE_GUIDE.md` | Rewrite with package-selection guidance |

## Get Started

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Install and Setup | How-to | `README.md`, solution and project files | Clear prerequisites and setup flow |
| Run Your First Workflow | How-to | `examples/01_hello_echo.yaml`, `src/Procedo.Runtime/Program.cs` | Walkthrough with expected output |
| Create Your First Workflow | How-to | `examples/01_hello_echo.yaml`, `examples/hello_pipeline.yaml` | New prose and YAML explanation |
| Procedo CLI Basics | How-to | `README.md`, `src/Procedo.Runtime/Program.cs` | Task-oriented command guide |
| Hello World Walkthrough | Recipe | `examples/01_hello_echo.yaml` | Narrative walkthrough |

## Author Workflows

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Workflow Structure Overview | Concept | `README.md`, `src/Procedo.Core/Models/` | New explanation and diagram |
| Stages | Reference | `src/Procedo.Core/Models/StageDefinition.cs`, examples | Dedicated syntax page |
| Jobs | Reference | `src/Procedo.Core/Models/JobDefinition.cs`, examples | Dedicated syntax page |
| Steps | Reference | `src/Procedo.Core/Models/StepDefinition.cs`, examples | Dedicated syntax page |
| Inputs with `with` | Reference | examples across `examples/` | New page |
| Parameters | Reference | `examples/48_template_parameters_demo.yaml`, `examples/49_parameter_schema_validation_demo.yaml`, `docs/VALIDATION.md` | Exact schema and behavior write-up |
| Variables | Reference | `examples/06_vars_expression_via_step.yaml`, `examples/48_template_parameters_demo.yaml` | New detailed guidance |
| Outputs | Reference | `examples/05_outputs_and_expressions.yaml` | Resolution rules and best practices |
| Expressions Overview | Concept | `src/Procedo.Expressions/`, examples 05, 53, 58 | New high-level explanation |
| Conditions | How-to/Reference | `examples/53_runtime_condition_demo.yaml`, `examples/58_runtime_expression_function_showcase.yaml` | Likely multi-page split later |
| Dependencies and Execution Order | Concept | `examples/02_linear_depends_on.yaml`, `examples/03_fan_out_fan_in.yaml`, `src/Procedo.Engine/` | New explanation and diagrams |

## Templates

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Templates Overview | Concept | `docs/TEMPLATES.md`, examples 48, 50, 54, 59, 60, 61, 62 | Rewrite and split |
| Template Parameters | Reference | `examples/48_template_parameters_demo.yaml` | Exact syntax page |
| Template Conditions | Reference | `examples/54_template_runtime_condition_demo.yaml`, `examples/59_branching_operator_showcase.yaml` | Exact operator behavior docs |
| Template Loops | Reference | `examples/59_branching_operator_showcase.yaml` | More dedicated examples likely needed |
| Template Inheritance | Reference | `docs/TEMPLATES.md`, template examples | Precise rules page |
| Template Limitations | Reference | `README.md`, `docs/KNOWN_LIMITATIONS.md` | Help-oriented wording |

## Run And Operate

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Execution Model | Concept | `src/Procedo.Engine/` | New explanation page |
| Runtime Options | Reference | `src/Procedo.Core/Execution/WorkflowExecutionOptions.cs`, runtime host | User-facing mapping |
| Persistence | How-to | `docs/PERSISTENCE.md`, examples 16, 17, 55 | Rewrite with lifecycle explanation |
| Resume Waiting Workflows | How-to | examples 45, 47, 61 | Walkthrough and troubleshooting |
| State Storage | Reference | `src/Procedo.Persistence/Stores/FileRunStateStore.cs` | New page |
| Observability | How-to | `docs/OBSERVABILITY.md`, examples 18, 19, 46 | Rewrite with expected output |
| Event Sinks | Reference | `src/Procedo.Observability/Sinks/` | New page |
| Validation | How-to | `docs/VALIDATION.md`, validation example app | More user-focused framing |
| Security Model | Concept/Reference | `docs/SECURITY_MODEL.md`, examples 43, 44 | Rewrite with practical recommendations |

## Extend Procedo

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Plugin Authoring Overview | Concept | `docs/PLUGIN_AUTHORING.md`, `src/Procedo.Plugin.SDK/` | Rewrite for user guidance |
| Create a Custom Step | How-to | `examples/Procedo.Example.CustomSteps` | Tested walkthrough |
| Register Plugins | How-to | plugin SDK and example apps | New step-by-step page |
| Method Binding | How-to/Reference | `docs/METHOD_BINDING.md`, custom steps example | Rewrite and possibly split |
| Dependency Injection Integration | How-to | `examples/Procedo.Example.DependencyInjection`, `src/Procedo.Extensions.DependencyInjection/` | User-focused prose |
| System Plugin and Built-in Capabilities | Concept | system examples 31-40, 51 | Dedicated overview page |

## Use In .NET

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Embedding Procedo | How-to | `docs/EMBEDDING_PROCEDO.md`, `examples/Procedo.Example.Basic` | Rewrite as task-based guide |
| ProcedoHostBuilder | Reference/How-to | `src/Procedo.Hosting/Hosting/ProcedoHostBuilder.cs`, extensible example | New focused page |
| Execute YAML from Code | How-to | basic example, `README.md` snippet | Code-first walkthrough |
| Configure Services | How-to | DI example, service builder code | New guide |
| Custom Runtime Composition | How-to | extensible example and host options | Needs curation |
| Testing Embedded Workflows | How-to | test projects and examples | Likely needs new examples |

## Reference

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| CLI Overview | Reference | runtime program, `README.md` commands | Command map and behavior notes |
| Run Command | Reference | runtime CLI behavior, example invocations | Exact options and examples |
| Resume Command | Reference | wait/resume examples | Exact behavior and expected states |
| Validate Command | Reference | validation docs and runtime host | Confirm exact CLI surface |
| Events and Output Options | Reference | observability docs and runtime options | Precise option reference |
| Parameters and Arguments | Reference | `README.md`, `examples/README.md` | Exact parsing rules |
| Workflow Schema Overview | Reference | core model classes, parser, validator | New top-level schema page |
| `name` and `version` | Reference | model classes, examples | Dedicated page |
| `parameters` | Reference | example 49, validation docs | Exact schema details |
| `stages` | Reference | model class and examples | Dedicated page |
| `jobs` | Reference | model class and examples | Dedicated page |
| `steps` | Reference | model class and examples | Dedicated page |
| `with` | Reference | many examples | Dedicated page |
| `condition` | Reference | examples 53, 58 | Exact evaluation notes |
| outputs and references | Reference | example 05, expression resolver | Precise syntax docs |
| Expressions Overview | Reference | expression resolver, examples | New page |
| Value Resolution | Reference | `src/Procedo.Expressions/WorkflowContextResolver.cs` | New page |
| Built-in Functions | Reference | expression resolver and example 58 | Full function catalog |
| Boolean Expressions | Reference | examples 53, 58 | New page |
| String and Collection Patterns | Reference | example 58 | New page |
| Common Expression Errors | Reference | expression exceptions and troubleshooting | New page |
| Built-in Steps Overview | Reference | system examples 31-40, 51 | New index page |
| `system.echo` | Reference | examples 01 and others | Straightforward page |
| process/system execution step | Reference | examples 40, 43, 44 | Needs security notes |
| wait/signal steps | Reference | examples 45, 47, 52, 61, 62 | Good source material |
| file/system utility steps | Reference | examples 32-39, 51, 57 | Should split into multiple pages |
| Run Status | Reference | `src/Procedo.Core/Runtime/RunStatus.cs` | Small focused page |
| Step Status | Reference | `src/Procedo.Core/Runtime/StepRunStatus.cs` | Small focused page |
| Wait Descriptors | Reference | `src/Procedo.Core/Runtime/WaitDescriptor.cs` | New page |
| Persistence State Model | Reference | `src/Procedo.Core/Runtime/WorkflowRunState.cs`, persistence store | New page |
| Resume Behavior | Reference | runtime logic and wait/resume examples | New page |
| Package pages | Reference | project files and `docs/PACKAGE_GUIDE.md` | Package-by-package usage guidance |
| Validation Errors | Reference | validation models, docs, examples | Error catalog |
| Runtime Errors | Reference | runtime error codes and examples | Error catalog |
| Error Codes | Reference | `src/Procedo.Core/Models/RuntimeErrorCodes.cs` | User-friendly descriptions |
| Troubleshooting by Error | Reference | `docs/TROUBLESHOOTING.md`, examples | Curated mapping |

## Recipes

| Page | Type | Current Source Material | Missing / Needs Expansion |
|---|---|---|---|
| Minimal Pipeline | Recipe | `examples/01_hello_echo.yaml` | Launch-ready |
| Conditional Execution | Recipe | `examples/53_runtime_condition_demo.yaml` | Launch-ready |
| Passing Data Between Steps | Recipe | `examples/05_outputs_and_expressions.yaml` | Launch-ready |
| Parameter Validation Patterns | Recipe | `examples/49_parameter_schema_validation_demo.yaml` | Good phase-2 candidate |
| Template Reuse Patterns | Recipe | examples 48 and 60 | More explanation |
| Wait/Resume Pattern | Recipe | `examples/45_wait_signal_demo.yaml` | Good phase-2 candidate |
| Logging and Events | Recipe | examples 18 and 19 | Good phase-2 candidate |
| Safe Production Runtime | Recipe | examples 43 and 44 | Good phase-2 candidate |
| Custom Step Cookbook | Recipe | `examples/Procedo.Example.CustomSteps` | Phase-3 candidate |
