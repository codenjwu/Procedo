# Known Limitations

Procedo Phase 1 is intentionally scoped for reliable single-node execution rather than full distributed orchestration.

## Runtime scope

- Single-node execution only
- No queue-backed scheduling, distributed leasing, or worker rebalancing
- No built-in multi-tenant isolation or sandboxing for untrusted workflows

## Persistence

- Persistence is local and file-backed
- Resume semantics are designed for local host recovery, not clustered failover
- Long-term state migration strategy is intentionally lightweight in Phase 1

## Templates and DSL

- Templates support one base template rather than arbitrary graph composition or fragment merging
- Template-time `${{ each }}` currently supports arrays, not object/key-value iteration
- Parameter schemas provide pragmatic constraint validation, not full JSON Schema expressiveness
- Workflow variables support `params.*` and `vars.*`; step outputs must use `steps.<stepId>.outputs.<key>`

## Security model

- `system.*` steps are intended for trusted-host scenarios
- Guardrails exist for files, HTTP, and process execution, but this is not a full sandbox
- Secret-management integration is not yet a first-class built-in feature

## Observability

- Structured execution events are available, but Phase 1 does not yet include a metrics/tracing backend story
- Local console and JSONL sinks are the reference observability path

## Packaging

- The public NuGet surface is intentionally narrowed to engine, hosting, plugin SDK, system plugin, and DI integration
- Internal implementation projects still exist in the repository, but they are not part of the intended public publish set
